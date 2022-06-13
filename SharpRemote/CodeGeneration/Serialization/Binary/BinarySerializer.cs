using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using SharpRemote.CodeGeneration;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.CodeGeneration.Serialization.Binary.Serializers;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     <see cref="ISerializer" /> implementation that just-in-time compiles the code responsible
	///     for serializing arbitrary types. <see cref="WriteObject" /> serializes an object graph to
	///     a <see cref="BinaryWriter" /> and <see cref="ReadObject" /> deserializes one from a <see cref="BinaryReader" />.
	/// </summary>
	/// <remarks>
	///     An object graph (or sub-graph) can only be serialized if its type is either:
	///     - Natively supported: <see cref="string" />, <see cref="TimeSpan" />, etc...
	///     - Attributed with the <see cref="DataContractAttribute" /> and <see cref="DataMemberAttribute" />
	/// </remarks>
	public sealed partial class BinarySerializer
		: ISerializerCompiler
	{
		private readonly ModuleBuilder _module;
		private readonly Dictionary<Type, SerializationMethods> _serializationMethods;
		private readonly List<IBuiltInTypeSerializer> _customSerializers;
		private readonly Dictionary<Type, MethodInfo> _getSingletonInstance;
		private readonly ITypeResolver _customTypeResolver;

		/// <summary>
		/// Creates a new serializer that dynamically compiles serialization methods to the given
		/// <see cref="ModuleBuilder"/>.
		/// </summary>
		/// <param name="module"></param>
		/// <param name="customTypeResolver">The instance of the type resolver, if any, that is used to resolve types upon deserialization</param>
		public BinarySerializer(ModuleBuilder module, ITypeResolver customTypeResolver = null)
		{
			if (module == null) throw new ArgumentNullException(nameof(module));

			_module = module;
			_customTypeResolver = new TypeResolverAdapter(customTypeResolver);
			_serializationMethods = new Dictionary<Type, SerializationMethods>();

			_customSerializers = new List<IBuiltInTypeSerializer>
			{
				new Int32Serializer(),
				new IPEndPointSerializer(),
				new IPAddressSerializer(),
				new BuiltInTypeSerializer(),
				new StringSerializer(),
				new ByteArraySerializer(),
				new TimeSpanSerializer(),
				new DateTimeSerializer(),
				new DateTimeOffsetSerializer(),
				new VersionSerializer(),
				new ApplicationIdSerializer(),
				new DecimalSerializer(),
				new UriSerializer(),
				new GuidSerializer(),
				new LevelSerializer(),

				// These serializers provide support for more than one type (for example generics)...
				new EnumSerializer(),
				new NullableSerializer(),
				new NullableSerializer(),
				new KeyValuePairSerializer(),
			};

			_getSingletonInstance = new Dictionary<Type, MethodInfo>();
		}

		/// <summary>
		/// The module where all newly created types reside in.
		/// </summary>
		public ModuleBuilder Module => _module;

		/// <summary>
		/// Creates a new serializer that dynamically compiles serialization methods to a new DynamicAssembly.
		/// </summary>
		public BinarySerializer(ITypeResolver typeResolver = null)
			: this(CreateModule(), typeResolver)
		{
		}

		/// <inheritdoc />
		public Type GetType(string assemblyQualifiedTypeName)
		{
			return _customTypeResolver.GetType(assemblyQualifiedTypeName);
		}

		/// <inheritdoc />
		public void RegisterType<T>()
		{
			Type type = typeof (T);
			RegisterType(type);
		}

		/// <inheritdoc />
		public void WriteObject(BinaryWriter writer, object value, IRemotingEndPoint remotingEndPoint)
		{
			if (value == null)
			{
				writer.Write("null");
			}
			else
			{
				Type type = value.GetType();
				Action<BinaryWriter, object, ISerializer, IRemotingEndPoint> fn = GetWriteObjectDelegate(type);
				fn(writer, value, this, remotingEndPoint);
			}
		}

		/// <inheritdoc />
		public object ReadObject(BinaryReader reader, IRemotingEndPoint remotingEndPoint)
		{
			string typeName = reader.ReadString();
			if (typeName != "null")
			{
				Type type = GetType(typeName);
				Func<BinaryReader, ISerializer, IRemotingEndPoint, object> fn = GetReadObjectDelegate(type);
				return fn(reader, this, remotingEndPoint);
			}

			return null;
		}

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");

#if NET6_0
			var access = AssemblyBuilderAccess.Run;
#else
			var access = AssemblyBuilderAccess.RunAndSave;
#endif

			AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
			string moduleName = assemblyName.Name + ".dll";
			ModuleBuilder module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		/// <summary>
		///     Returns the method to write a value of the given type to a writer.
		///     Signature: WriteSealed(BinaryWriter writer, T value, ISerializer serializer)
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public MethodInfo GetWriteValueMethodInfo(Type type)
		{
			// We can make a certain distinction at compile time that helps to reduce the amount of dynamic lookups:
			// If a type is sealed (value type or sealed class) then its instance IS EXACTLY the type we know at
			// compile time and thus we can directly embedd the call to its serialization method.
			if (type.IsValueType || type.IsSealed)
			{
				SerializationMethods methods;
				RegisterType(type, out methods);
				return methods.WriteValueMethod;
			}

			// If we know that the given type is serialized by reference (as opposed to by value) then
			// it doesn't matter which ACTUAL type we're dealing with because all types implementing
			// a [ByReference] interface must be serialized by reference, without exception.
			if (type.GetRealCustomAttribute<ByReferenceAttribute>(true) != null)
			{
				// Serializing by reference when, at compile time, it is known that the object is to
				// be serialized by reference only requires storing its grain-object-id in the stream.
				var interfaceType = FindProxyInterface(type);
				SerializationMethods methods;
				RegisterType(interfaceType, out methods);
				return methods.WriteValueMethod;
			}

			// We don't know the true type of the parameter until we inspect it's actual value.
			// Thus we're forced to do a dynamic dispatch.
			return Methods.SerializerWriteObject;
		}

		/// <summary>
		///     Returns the method to write a value of the given type to a writer.
		///     Signature: ReadSealed(BinaryReader reader, ISerializer serializer)
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public MethodInfo GetReadValueMethodInfo(Type type)
		{
			// We can make a certain distinction at compile time that helps to reduce the amount of dynamic lookups:
			// If a type is sealed (value type or sealed class) then its instance IS EXACTLY the type we know at
			// compile time and thus we can directly embedd the call to its serialization method.
			if (type.IsValueType || type.IsSealed)
			{
				SerializationMethods methods;
				RegisterType(type, out methods);
				return methods.ReadValueMethod;
			}

			// If we know that the given type is serialized by reference (as opposed to by value) then
			// it doesn't matter which ACTUAL type we're dealing with because all types implementing
			// a [ByReference] interface must be serialized by reference, without exception.
			if (type.GetRealCustomAttribute<ByReferenceAttribute>(true) != null)
			{
				// Serializing by reference when, at compile time, it is known that the object is to
				// be serialized by reference only requires storing its grain-object-id in the stream.
				var interfaceType = FindProxyInterface(type);
				SerializationMethods methods;
				RegisterType(interfaceType, out methods);
				return methods.ReadValueMethod;
			}

			// We don't know the true type of the parameter until we inspect it's actual value.
			// Thus we're forced to do a dynamic dispatch.
			return Methods.SerializerReadObject;
		}

		private Action<BinaryWriter, object, ISerializer, IRemotingEndPoint> GetWriteObjectDelegate(Type type)
		{
			SerializationMethods methods;
			RegisterType(type, out methods);
			return methods.WriteDelegate;
		}

		private Func<BinaryReader, ISerializer, IRemotingEndPoint, object> GetReadObjectDelegate(Type type)
		{
			SerializationMethods methods;
			RegisterType(type, out methods);

			return methods.ReadObjectDelegate;
		}

		private SerializationMethods CreateSerializationMethods(TypeInformation typeInformation)
		{
			string typeName = BuildTypeName(typeInformation);
			TypeBuilder typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			MethodBuilder writeValueNotNullMethod = typeBuilder.DefineMethod("WriteValueNotNull",
			                                                                 MethodAttributes.Public | MethodAttributes.Static,
			                                                                 CallingConventions.Standard, typeof (void), new[]
				                                                                 {
					                                                                 typeof (BinaryWriter),
					                                                                 typeInformation.Type,
					                                                                 typeof (ISerializer),
					                                                                 typeof (IRemotingEndPoint)
				                                                                 });

			MethodInfo writeObjectMethod = CreateWriteValueWithTypeInformation(typeBuilder, writeValueNotNullMethod, typeInformation.Type);
			MethodInfo writeValueMethod = CreateWriteValue(typeBuilder, writeValueNotNullMethod, typeInformation.Type);
			MethodBuilder readValueNotNullMethod = typeBuilder.DefineMethod("ReadValueNotNull",
			                                                                MethodAttributes.Public | MethodAttributes.Static,
			                                                                CallingConventions.Standard, typeInformation.Type,
			                                                                new[]
				                                                                {
					                                                                typeof (BinaryReader),
					                                                                typeof (ISerializer),
					                                                                typeof (IRemotingEndPoint)
				                                                                });

			MethodInfo readObjectMethod = CreateReadObject(typeBuilder, readValueNotNullMethod, typeInformation.Type);
			MethodInfo readValueMethod = CreateReadValue(typeBuilder, readValueNotNullMethod, typeInformation.Type);

			var m = new SerializationMethods(
				writeValueMethod,
				writeObjectMethod,
				readValueMethod,
				readObjectMethod);
			_serializationMethods.Add(typeInformation.Type, m);

			try
			{
				EmitWriteValueNotNullMethod(writeValueNotNullMethod.GetILGenerator(), typeInformation);
				EmitReadValueNotNullMethod(readValueNotNullMethod.GetILGenerator(), typeInformation);

				typeBuilder.CreateType();

				m.WriteDelegate =
					(Action<BinaryWriter, object, ISerializer, IRemotingEndPoint>)
					typeBuilder.GetMethod("WriteObject").CreateDelegate(typeof(Action<BinaryWriter, object, ISerializer, IRemotingEndPoint>));

				m.ReadObjectDelegate =
					(Func<BinaryReader, ISerializer, IRemotingEndPoint, object>)
					typeBuilder.GetMethod("ReadObject").CreateDelegate(typeof(Func<BinaryReader, ISerializer, IRemotingEndPoint, object>));

				return m;
			}
			catch (Exception)
			{
				_serializationMethods.Remove(typeInformation.Type);
				throw;
			}
		}

		private static void BuildTypeName(StringBuilder builder, TypeInformation typeInformation)
		{
#if NET6_0
			builder.AppendFormat("{0}.{1}.Serialization", typeInformation.Namespace, typeInformation.Name);
#else
			builder.AppendFormat("{0}.{1}", typeInformation.Namespace, typeInformation.Name);
#endif
			if (typeInformation.IsGenericType)
			{
				builder.Append("[");
				var args = typeInformation.GenericArguments;
				for(int i = 0; i < args.Length; ++i)
				{
					if (i != 0)
						builder.Append(",");

					var type = args[i];
					BuildTypeName(builder, new TypeInformation(type));
				}
				builder.Append("]");
			}
		}

		private static string BuildTypeName(TypeInformation typeInformation)
		{
			var builder = new StringBuilder();
			BuildTypeName(builder, typeInformation);
			return builder.ToString();
		}

		private void EmitReadValueNotNullMethod(ILGenerator gen,
			TypeInformation typeInformation)
		{
			Action loadReader = () => gen.Emit(OpCodes.Ldarg_0);
			Action loadSerializer = () => gen.Emit(OpCodes.Ldarg_1);
			Action loadRemotingEndPoint = () => gen.Emit(OpCodes.Ldarg_2);

			MethodInfo method;
			if (IsSingleton(typeInformation.Type, out method))
			{
				EmitReadSingleton(gen, method);
			}
			else if (EmitReadNativeType(gen,
			                            loadReader,
			                            loadSerializer,
			                            loadRemotingEndPoint,
			                            typeInformation.Type,
			                            false))
			{
			}
			else if (typeInformation.IsArray)
			{
				EmitReadArray(gen,
				              loadReader,
				              loadSerializer,
				              loadRemotingEndPoint,
				              typeInformation);
			}
			else if (typeInformation.IsCollection)
			{
				EmitReadCollection(gen,
				                   loadReader,
				                   loadSerializer,
				                   loadRemotingEndPoint,
				                   typeInformation);
			}
			else if (typeInformation.IsStack)
			{
				EmitReadStack(gen,
				              loadReader,
				              loadSerializer,
				              loadRemotingEndPoint,
				              typeInformation);
			}
			else if (typeInformation.IsQueue)
			{
				EmitReadQueue(gen,
				              loadReader,
				              loadSerializer,
				              loadRemotingEndPoint,
				              typeInformation);
			}
			else
			{
				LocalBuilder value = gen.DeclareLocal(typeInformation.Type);
				EmitReadCustomType(gen,
				                   loadReader,
				                   loadSerializer,
				                   loadRemotingEndPoint,
				                   typeInformation,
				                   value);
			}

			gen.Emit(OpCodes.Ret);
		}

		private MethodInfo CreateReadValue(TypeBuilder typeBuilder, MethodBuilder readValueNotNull, Type type)
		{
			if (type.IsValueType)
				return readValueNotNull;

			MethodBuilder method = typeBuilder.DefineMethod("ReadValue", MethodAttributes.Public | MethodAttributes.Static,
			                                                CallingConventions.Standard, type, new[]
				                                                {
					                                                typeof (BinaryReader),
					                                                typeof (ISerializer),
					                                                typeof (IRemotingEndPoint)
				                                                });

			ILGenerator gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ReadBool);
			Label end = gen.DefineLabel();
			Label @null = gen.DefineLabel();
			gen.Emit(OpCodes.Brfalse, @null);

			// ReadValueNotNull(reader, serializer, remotingEndPoint);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Call, readValueNotNull);
			gen.Emit(OpCodes.Br_S, end);

			gen.MarkLabel(@null);
			gen.Emit(OpCodes.Ldnull);

			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private MethodInfo CreateReadObject(TypeBuilder typeBuilder, MethodBuilder readValueNotNull, Type type)
		{
			MethodBuilder method = typeBuilder.DefineMethod("ReadObject", MethodAttributes.Public | MethodAttributes.Static,
			                                                CallingConventions.Standard, typeof (object), new[]
				                                                {
					                                                typeof (BinaryReader),
					                                                typeof (ISerializer),
					                                                typeof (IRemotingEndPoint)
				                                                });

			bool requiresBoxing = type.IsPrimitive || type.IsValueType;
			ILGenerator gen = method.GetILGenerator();

			// return ReadValueNotNull(reader, serializer, remoteEndPoint);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Call, readValueNotNull);

			if (requiresBoxing)
			{
				gen.Emit(OpCodes.Box, type);
			}

			gen.Emit(OpCodes.Ret);
			return method;
		}

		private void EmitWriteValueNotNullMethod(ILGenerator gen, TypeInformation typeInformation)
		{
			Action loadWriter = () => gen.Emit(OpCodes.Ldarg_0);
			Action loadValue = () => gen.Emit(OpCodes.Ldarg_1);
			Action loadValueAddress = () => gen.Emit(OpCodes.Ldarga, 1);
			Action loadSerializer = () => gen.Emit(OpCodes.Ldarg_2);
			Action loadRemotingEndPoint = () => gen.Emit(OpCodes.Ldarg_3);

			MethodInfo method;
			if (IsSingleton(typeInformation.Type, out method))
			{
				// Nothing to do, all possible instance information has already been written....
			}
			else if (EmitWriteNativeType(
				gen,
				loadWriter,
				loadValue,
				loadValueAddress,
				loadSerializer,
				loadRemotingEndPoint,
				typeInformation.Type,
				false))
			{
			}
			else if (typeInformation.IsArray)
			{
				EmitWriteArray(gen, typeInformation, loadWriter, loadValue, loadSerializer, loadRemotingEndPoint);
			}
			else if (typeInformation.IsCollection)
			{
				EmitWriteCollection(gen, typeInformation, loadWriter, loadValue, loadSerializer, loadRemotingEndPoint);
			}
			else if (typeInformation.IsStack)
			{
				EmitWriteStack(gen, typeInformation, loadWriter, loadValue, loadSerializer, loadRemotingEndPoint);
			}
			else if (typeInformation.IsQueue)
			{
				EmitWriteQueue(gen, typeInformation, loadWriter, loadValue, loadSerializer, loadRemotingEndPoint);
			}
			else
			{
				WriteCustomType(gen, typeInformation.Type, loadWriter, loadRemotingEndPoint);
			}

			gen.Emit(OpCodes.Ret);
		}

		private MethodInfo CreateWriteValue(TypeBuilder typeBuilder, MethodBuilder valueNotNullMethod, Type type)
		{
			if (type.IsValueType)
				return valueNotNullMethod;

			MethodBuilder method = typeBuilder.DefineMethod("WriteValue", MethodAttributes.Public | MethodAttributes.Static,
			                                                CallingConventions.Standard, typeof (void), new[]
				                                                {
					                                                typeof (BinaryWriter),
					                                                type,
					                                                typeof (ISerializer),
					                                                typeof (IRemotingEndPoint)
				                                                });

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder result = gen.DeclareLocal(type);

			// if (object == null)
			Label @true = gen.DefineLabel();
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc, result);
			gen.Emit(OpCodes.Ldloc, result);
			gen.Emit(OpCodes.Brtrue, @true);

			// { writer.Write(false); }
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Call, Methods.WriteBool);

			Label end = gen.DefineLabel();
			gen.Emit(OpCodes.Br, end);

			// else { writer.Write(true); <Serialize Fields> }
			gen.MarkLabel(@true);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Call, Methods.WriteBool);

			// WriteValueNotNull(writer, value, serializer, remotingEndPoint)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Call, valueNotNullMethod);

			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private MethodInfo CreateWriteValueWithTypeInformation(TypeBuilder typeBuilder, MethodInfo writeValueNotNull, Type type)
		{
			MethodBuilder method = typeBuilder.DefineMethod("WriteObject", MethodAttributes.Public | MethodAttributes.Static,
			                                                CallingConventions.Standard, typeof (void), new[]
				                                                {
					                                                typeof (BinaryWriter),
					                                                typeof (object),
					                                                typeof (ISerializer),
					                                                typeof (IRemotingEndPoint)
				                                                });
			ILGenerator gen = method.GetILGenerator();

			EmitWriteTypeInformationOrNull(gen, () =>
				{
					// WriteValueNotNull(writer, value, serializer, remotingEndPoint);
					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldarg_1);

					if (type.IsPrimitive || type.IsValueType)
					{
						gen.Emit(OpCodes.Unbox_Any, type);
					}
					else
					{
						gen.Emit(OpCodes.Castclass, type);
					}

					gen.Emit(OpCodes.Ldarg_2);
					gen.Emit(OpCodes.Ldarg_3);
					gen.Emit(OpCodes.Call, writeValueNotNull);
				}, type);

			return method;
		}

		private void EmitWriteTypeInformationOrNull(ILGenerator gen, Action writeValue, Type type)
		{
			//gen.EmitWriteLine("writing type info");

			LocalBuilder result = gen.DeclareLocal(typeof (bool));

			// if (object == null)
			Label @true = gen.DefineLabel();
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc, result);
			gen.Emit(OpCodes.Ldloc, result);
			gen.Emit(OpCodes.Brtrue, @true);

			// { writer.WriteString(string.Empty); }
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldsfld, Methods.StringEmpty);
			gen.Emit(OpCodes.Call, Methods.WriteString);
			Label end = gen.DefineLabel();
			gen.Emit(OpCodes.Br, end);

			// else { writer.WriteString(type.AssemblyQualifiedName);
			gen.MarkLabel(@true);
			//gen.EmitWriteLine("writer.WriteString(type.AssemblyQualifiedName)");
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, type.AssemblyQualifiedName);
			gen.Emit(OpCodes.Call, Methods.WriteString);

			writeValue();

			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);

			//gen.EmitWriteLine("Type info written");
		}

		/// <inheritdoc />
		public void RegisterType(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			SerializationMethods unused;
			RegisterType(type, out unused);
		}

		[Pure]
		private Type PatchType(Type type)
		{
			if (type.Is(typeof (Type)))
				return typeof (Type);

			if (type.GetRealCustomAttribute<ByReferenceAttribute>(true) != null)
			{
				// Before we accept that this type is ByReference, we should
				// verify that no other constraints are broken (such as also being
				// a singleton type).
				MethodInfo unused;
				if (IsSingleton(type, out unused))
				{
					return type;
				}

				return FindProxyInterface(type);
			}

			return type;
		}

		internal void RegisterType<T>(out SerializationMethods serializationMethods)
		{
			RegisterType(typeof (T), out serializationMethods);
		}

		/// <summary>
		/// TODO: Replace with <see cref="SerializationMethodStorage{T}.GetOrAdd"/>.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="serializationMethods"></param>
		internal void RegisterType(Type type, out SerializationMethods serializationMethods)
		{
			lock (_serializationMethods)
			{
				// Usually we already have generated the methods necessary to serialize / deserialize
				// and thus we can simply retrieve them from the dictionary
				if (!_serializationMethods.TryGetValue(type, out serializationMethods))
				{
					// If that's not the case, then we'll have to generate them.
					// However we need to pay special attention to certain types, for example ByReference
					// types where the serialization method is IDENTICAL for each implementation.
					//
					// Usually we would call PatchType() everytime, however this method is very time-expensive
					// and therefore we will register both the type as well as the patched type, which
					// causes subsequent calls to RegisterType to no longer invoke PatchType.
					//
					// In essence PatchType is only ever invoked ONCE per type instead of for every call to RegisterType.
					var patchedType = PatchType(type);
					if (!_serializationMethods.TryGetValue(patchedType, out serializationMethods))
					{
						var typeInfo = new TypeInformation(patchedType);
						serializationMethods = CreateSerializationMethods(typeInfo);

						if (type != patchedType)
						{
							_serializationMethods.Add(type, serializationMethods);
						}
					}
				}
			}
		}

		/// <inheritdoc />
		[Pure]
		public bool IsTypeRegistered<T>()
		{
			return IsTypeRegistered(typeof (T));
		}

		/// <inheritdoc />
		[Pure]
		public bool IsTypeRegistered(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			lock (_serializationMethods)
			{
				return _serializationMethods.ContainsKey(type);
			}
		}

		internal sealed class SerializationMethods
		{
			/// <summary>
			///     Writes a value that can be null.
			/// </summary>
			public readonly MethodInfo WriteValueMethod;

			public readonly MethodInfo WriteObjectMethod;
			public readonly MethodInfo ReadValueMethod;
			public readonly MethodInfo ReadObjectMethod;
			public Func<BinaryReader, ISerializer, IRemotingEndPoint, object> ReadObjectDelegate;
			public Action<BinaryWriter, object, ISerializer, IRemotingEndPoint> WriteDelegate;

			public SerializationMethods(
				MethodInfo writeValueMethod,
				MethodInfo writeObjectMethod,
				MethodInfo readValueMethod,
				MethodInfo readObjectMethod)
			{
				if (writeValueMethod == null) throw new ArgumentNullException(nameof(writeValueMethod));
				if (writeObjectMethod == null) throw new ArgumentNullException(nameof(writeObjectMethod));
				if (readValueMethod == null) throw new ArgumentNullException(nameof(readValueMethod));
				if (readObjectMethod == null) throw new ArgumentNullException(nameof(readObjectMethod));

				WriteValueMethod = writeValueMethod;
				WriteObjectMethod = writeObjectMethod;
				ReadValueMethod = readValueMethod;
				ReadObjectMethod = readObjectMethod;
			}
		}
	}
}