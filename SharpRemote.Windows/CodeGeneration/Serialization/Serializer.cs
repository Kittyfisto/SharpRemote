using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using SharpRemote.CodeGeneration;
using SharpRemote.CodeGeneration.Serialization.Serializers;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// <see cref="ISerializer"/> implementation that just-in-time compiles the code responsible
	/// for serializing arbitrary types. <see cref="WriteObject"/> serializes an object graph to
	/// a <see cref="BinaryWriter"/> and <see cref="ReadObject"/> deserializes one from a <see cref="BinaryReader"/>.
	/// </summary>
	/// <remarks>
	/// An object graph (or sub-graph) can only be serialized if its type is either:
	/// - Natively supported: <see cref="string"/>, <see cref="TimeSpan"/>, etc...
	/// - Attributed with the <see cref="DataContractAttribute"/> and <see cref="DataMemberAttribute"/>
	/// </remarks>
	internal sealed partial class Serializer
		: ISerializer
	{
		private readonly ModuleBuilder _module;
		private readonly Dictionary<Type, SerializationMethods> _serializationMethods;
		private readonly List<ITypeSerializer> _customSerializers;
		private readonly Dictionary<Type, MethodInfo> _getSingletonInstance;
		private readonly ITypeResolver _customTypeResolver;

		/// <summary>
		/// Creates a new serializer that dynamically compiles serialization methods to the given
		/// <see cref="ModuleBuilder"/>.
		/// </summary>
		/// <param name="module"></param>
		/// <param name="customTypeResolver">The instance of the type resolver, if any, that is used to resolve types upon deserialization</param>
		public Serializer(ModuleBuilder module, ITypeResolver customTypeResolver = null)
		{
			if (module == null) throw new ArgumentNullException("module");

			_module = module;
			_customTypeResolver = customTypeResolver;
			_serializationMethods = new Dictionary<Type, SerializationMethods>();

			_customSerializers = new List<ITypeSerializer>
			{
				new Int32Serializer(),
				new IPEndPointSerializer(),
				new IPAddressSerializer(),
				new TypeSerializer(),
				new StringSerializer(),
				new ByteArraySerializer(),
				new TimeSpanSerializer(),
				new DateTimeSerializer(),
				new DateTimeOffsetSerializer(),
				new VersionSerializer(),
				new ApplicationIdSerializer(),
				new UriSerializer(),
				new GuidSerializer(),

				// These serializers provide support for more than one type (for example generics)...
				new EnumSerializer(),
				new NullableSerializer(),
				new KeyValuePairSerializer(),
			};

			_getSingletonInstance = new Dictionary<Type, MethodInfo>();
		}

		/// <summary>
		/// Creates a new serializer that dynamically compiles serialization methods to a new DynamicAssembly.
		/// </summary>
		public Serializer(ITypeResolver typeResolver = null)
			: this(CreateModule(), typeResolver)
		{
		}

		public Type GetType(string assemblyQualifiedTypeName)
		{
			if (_customTypeResolver != null)
				return _customTypeResolver.GetType(assemblyQualifiedTypeName);

			return TypeResolver.GetType(assemblyQualifiedTypeName);
		}

		public void RegisterType<T>()
		{
			Type type = typeof (T);
			RegisterType(type);
		}

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
			AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
			                                                                         AssemblyBuilderAccess.RunAndSave);
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
			if (type.IsValueType || type.IsSealed)
			{
				SerializationMethods methods;
				RegisterType(type, out methods);
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
			if (type.IsValueType || type.IsSealed)
			{
				SerializationMethods methods;
				RegisterType(type, out methods);
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

			CreateWriteValueWithTypeInformation(typeBuilder, writeValueNotNullMethod, typeInformation.Type);
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

			CreateReadObject(typeBuilder, readValueNotNullMethod, typeInformation.Type);
			MethodInfo readValueMethod = CreateReadValue(typeBuilder, readValueNotNullMethod, typeInformation.Type);

			var m = new SerializationMethods(
				writeValueMethod,
				readValueMethod);
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
			builder.AppendFormat("{0}.{1}", typeInformation.Namespace, typeInformation.Name);
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
			if (IsSingleton(typeInformation, out method))
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

		private void CreateReadObject(TypeBuilder typeBuilder, MethodBuilder readValueNotNull, Type type)
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
		}

		private void EmitWriteValueNotNullMethod(ILGenerator gen, TypeInformation typeInformation)
		{
			Action loadWriter = () => gen.Emit(OpCodes.Ldarg_0);
			Action loadValue = () => gen.Emit(OpCodes.Ldarg_1);
			Action loadValueAddress = () => gen.Emit(OpCodes.Ldarga, 1);
			Action loadSerializer = () => gen.Emit(OpCodes.Ldarg_2);
			Action loadRemotingEndPoint = () => gen.Emit(OpCodes.Ldarg_3);

			MethodInfo method;
			if (IsSingleton(typeInformation, out method))
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

			Label @end = gen.DefineLabel();
			gen.Emit(OpCodes.Br, @end);

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

			gen.MarkLabel(@end);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private void CreateWriteValueWithTypeInformation(TypeBuilder typeBuilder, MethodInfo writeValueNotNull, Type type)
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
				});
		}

		private void EmitWriteTypeInformation(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, Methods.ObjectGetType);
			gen.Emit(OpCodes.Callvirt, TypeSerializer.GetAssemblyQualifiedName);
			gen.Emit(OpCodes.Call, Methods.WriteString);
		}

		private void EmitWriteTypeInformationOrNull(ILGenerator gen, Action writeValue)
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
			Label @end = gen.DefineLabel();
			gen.Emit(OpCodes.Br, @end);

			// else { writer.WriteString(object.GetType().AssemblyQualifiedName);
			gen.MarkLabel(@true);
			//gen.EmitWriteLine("writer.WriteString(object.GetType().AssemblyQualifiedName)");
			EmitWriteTypeInformation(gen);

			writeValue();

			gen.MarkLabel(@end);
			gen.Emit(OpCodes.Ret);

			//gen.EmitWriteLine("Type info written");
		}

		public void RegisterType(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			SerializationMethods unused;
			RegisterType(type, out unused);
		}

		[Pure]
		private Type PatchType(Type type)
		{
			if (type.Is(typeof (Type)))
				return typeof (Type);

			return type;
		}

		private void RegisterType(Type type, out SerializationMethods serializationMethods)
		{
			type = PatchType(type);
			if (!_serializationMethods.TryGetValue(type, out serializationMethods))
			{
				var typeInfo = new TypeInformation(type);
				serializationMethods = CreateSerializationMethods(typeInfo);
			}
		}

		[Pure]
		public bool IsTypeRegistered<T>()
		{
			return IsTypeRegistered(typeof (T));
		}

		[Pure]
		public bool IsTypeRegistered(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return _serializationMethods.ContainsKey(type);
		}

		private sealed class SerializationMethods
		{
			/// <summary>
			///     Writes a value that can be null.
			/// </summary>
			public readonly MethodInfo WriteValueMethod;
			public readonly MethodInfo ReadValueMethod;
			public Func<BinaryReader, ISerializer, IRemotingEndPoint, object> ReadObjectDelegate;
			public Action<BinaryWriter, object, ISerializer, IRemotingEndPoint> WriteDelegate;

			public SerializationMethods(
				MethodInfo writeValueMethod,
				MethodInfo readValueMethod)
			{
				if (writeValueMethod == null) throw new ArgumentNullException("writeValueMethod");

				if (readValueMethod == null) throw new ArgumentNullException("readValueMethod");

				WriteValueMethod = writeValueMethod;
				ReadValueMethod = readValueMethod;
			}
		}
	}
}