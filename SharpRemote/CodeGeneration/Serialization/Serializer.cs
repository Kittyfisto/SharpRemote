using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization.Serializers;

namespace SharpRemote.CodeGeneration.Serialization
{
	public sealed partial class Serializer
		: ISerializer
	{
		private readonly ModuleBuilder _module;
		private readonly Dictionary<Type, WriteMethod> _typeToWriteMethods;
		private readonly Dictionary<Type, ReadMethod> _typeToReadMethods;

		sealed class ReadMethod
		{
			public readonly MethodInfo ReadValueMethod;
			public Func<BinaryReader, ISerializer, object> ReadObjectDelegate;
			public MethodInfo ReadObjectMethod;

			public ReadMethod(MethodBuilder readValueMethod)
			{
				ReadValueMethod = readValueMethod;
			}
		}

		sealed class WriteMethod
		{
			/// <summary>
			/// The method that takes a parameter of the actual type in question.
			/// </summary>
			public readonly MethodInfo ValueMethod;

			/// <summary>
			/// The method that takes an object parameter.
			/// </summary>
			public readonly MethodInfo ObjectMethod;

			public Action<BinaryWriter, object, ISerializer> WriteDelegate;

			public WriteMethod(MethodBuilder valueMethod, MethodInfo objectMethod)
			{
				if (valueMethod == null) throw new ArgumentNullException("valueMethod");
				if (objectMethod == null) throw new ArgumentNullException("objectMethod");

				ValueMethod = valueMethod;
				ObjectMethod = objectMethod;
			}
		}

		public Serializer(ModuleBuilder module)
		{
			if (module == null) throw new ArgumentNullException("module");

			_module = module;
			_typeToWriteMethods = new Dictionary<Type, WriteMethod>();
			_typeToReadMethods = new Dictionary<Type, ReadMethod>();
		}

		public Serializer()
			: this(CreateModule())
		{}

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		/*
		/// <summary>
		/// Writes the current value on top of the evaluation stack onto the binary writer that's
		/// second to top on the evaluation stack.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadWriter"></param>
		/// <param name="loadValue"></param>
		/// <param name="valueType"></param>
		/// <param name="serializer"></param>
		public void EmitWriteValue(ILGenerator gen,
			Action loadWriter,
			Action loadValue,
			Type valueType,
			FieldBuilder serializer)
		{
			if (!gen.EmitWriteNativeType(loadWriter, loadValue, valueType))
			{
				var writeObject = GetWriteValueMethodInfo(valueType);

				// Serializer.Serialize(writer, value, this.serializer)
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, serializer);
				gen.Emit(OpCodes.Call, writeObject);
			}
		}*/

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
				WriteMethod method;
				ReadMethod unused;
				RegisterType(type, out method, out unused);
				return method.ValueMethod;
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
				WriteMethod unused;
				ReadMethod method;
				RegisterType(type, out unused, out method);
				return method.ReadValueMethod;
			}

			// We don't know the true type of the parameter until we inspect it's actual value.
			// Thus we're forced to do a dynamic dispatch.
			return Methods.SerializerReadObject;
		}

		private Action<BinaryWriter, object, ISerializer> GetWriteObjectDelegate(Type type)
		{
			WriteMethod method;
			ReadMethod unused;
			RegisterType(type, out method, out unused);
			return method.WriteDelegate;
		}

		private Func<BinaryReader, ISerializer, object> GetReadObjectDelegate(Type type)
		{
			WriteMethod unused;
			ReadMethod method;
			RegisterType(type, out unused, out method);

			return method.ReadObjectDelegate;
		}

		private ReadMethod CompileReadMethod(TypeInformation typeInformation)
		{
			var typeName = string.Format("Read.{0}.{1}", typeInformation.Namespace, typeInformation.Name);
			var typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			var method = typeBuilder.DefineMethod("ReadValue", MethodAttributes.Public | MethodAttributes.Static,
												   CallingConventions.Standard, typeInformation.Type, new[]
				                                       {
														   typeof(BinaryReader),
														   typeof (ISerializer)
				                                       });

			CreateReadDelegate(typeBuilder, method, typeInformation.Type);
			var m = new ReadMethod(method);
			_typeToReadMethods.Add(typeInformation.Type, m);

			var gen = method.GetILGenerator();
			if (typeInformation.IsArray)
			{
				EmitReadArray(gen, typeInformation);
			}
			else if (gen.EmitReadNativeType(() => gen.Emit(OpCodes.Ldarg_0),
					typeInformation.Type))
			{
				
			}
			else if (typeInformation.IsValueType)
			{
				var value = gen.DeclareLocal(typeInformation.Type);
				EmitReadValueType(gen, typeInformation, value);
			}
			else if (typeInformation.IsSealed)
			{
				var value = gen.DeclareLocal(typeInformation.Type);
				EmitReadSealedClass(gen, typeInformation, value);
			}
			else
			{
				EmitReadValue(gen,
					() => gen.Emit(OpCodes.Ldarg_0),
					() => gen.Emit(OpCodes.Ldarg_1),
					typeInformation.Type);
			}

			gen.Emit(OpCodes.Ret);

			var serializerType = typeBuilder.CreateType();
			m.ReadObjectMethod = serializerType.GetMethod("ReadObject", new[] { typeof(BinaryReader), typeof(ISerializer) });
			m.ReadObjectDelegate = (Func<BinaryReader, ISerializer, object>)m.ReadObjectMethod.CreateDelegate(typeof(Func<BinaryReader, ISerializer, object>));

			return m;
		}

		private void CreateReadDelegate(TypeBuilder typeBuilder, MethodBuilder methodInfo, Type type)
		{
			var method = typeBuilder.DefineMethod("ReadObject", MethodAttributes.Public | MethodAttributes.Static,
												   CallingConventions.Standard, typeof(object), new[]
				                                       {
														   typeof (BinaryReader),
														   typeof (ISerializer)
				                                       });

			bool requiresBoxing = type.IsPrimitive || type.IsValueType;
			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, methodInfo);

			if (requiresBoxing)
			{
				gen.Emit(OpCodes.Box, type);
			}
			else
			{
				gen.Emit(OpCodes.Castclass, type);
			}

			gen.Emit(OpCodes.Ret);
		}

		private WriteMethod CompileWriteMethod(TypeInformation typeInformation)
		{
			var typeName = string.Format("Write.{0}.{1}", typeInformation.Namespace, typeInformation.Name);
			var typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			var valueMethod = typeBuilder.DefineMethod("WriteValue", MethodAttributes.Public | MethodAttributes.Static,
			                                       CallingConventions.Standard, typeof (void), new[]
				                                       {
														   typeof(BinaryWriter),
					                                       typeInformation.Type,
														   typeof (ISerializer)
				                                       });
			var objectMethod = CreateWriteDelegate(typeBuilder, valueMethod, typeInformation.Type);
			var m = new WriteMethod(valueMethod, objectMethod);
			_typeToWriteMethods.Add(typeInformation.Type, m);

			var gen = valueMethod.GetILGenerator();

			if (typeInformation.IsArray)
			{
				EmitWriteArray(gen, typeInformation);
			}
			else if (gen.EmitWriteNativeType(
				() => gen.Emit(OpCodes.Ldarg_0),
				() => gen.Emit(OpCodes.Ldarg_1),
				typeInformation.Type))
			{
			}
			else if (typeInformation.IsValueType)
			{
				WriteValueType(gen, typeInformation);
			}
			else if (typeInformation.IsSealed)
			{
				WriteSealedObject(gen, typeInformation.Type);
			}
			else
			{
				WriteUnsealedObject(gen, typeInformation.Type);
			}

			gen.Emit(OpCodes.Ret);

			var serializerType = typeBuilder.CreateType();
			var delegateMethod = serializerType.GetMethod("WriteObject", new[] { typeof(BinaryWriter), typeof(object), typeof(ISerializer) });
			m.WriteDelegate = (Action<BinaryWriter, object, ISerializer>)delegateMethod.CreateDelegate(typeof(Action<BinaryWriter, object, ISerializer>));

			return m;
		}

		private MethodInfo CreateWriteDelegate(TypeBuilder typeBuilder, MethodInfo methodInfo, Type type)
		{
			var method = typeBuilder.DefineMethod("WriteObject", MethodAttributes.Public | MethodAttributes.Static,
												   CallingConventions.Standard, typeof(void), new[]
				                                       {
														   typeof(BinaryWriter),
					                                       typeof(object),
														   typeof (ISerializer)
				                                       });
			var gen = method.GetILGenerator();

			EmitWriteTypeInformationOrNull(gen, () =>
			{
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
				gen.Emit(OpCodes.Call, methodInfo);
			});

			return method;
		}

		private void EmitWriteTypeInformation(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, Methods.ObjectGetType);
			gen.Emit(OpCodes.Callvirt, TypeSerializer.GetAssemblyQualifiedName);
			gen.Emit(OpCodes.Callvirt, Methods.WriteString);
		}

		private void EmitWriteTypeInformationOrNull(ILGenerator gen, Action writeValue)
		{
			//gen.EmitWriteLine("writing type info");

			var result = gen.DeclareLocal(typeof(bool));

			// if (object == null)
			var @true = gen.DefineLabel();
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
			gen.Emit(OpCodes.Callvirt, Methods.WriteString);
			var @end = gen.DefineLabel();
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

		public void RegisterType<T>()
		{
			var type = typeof (T);
			RegisterType(type);
		}

		public void RegisterType(Type type)
		{
			WriteMethod unused1;
			ReadMethod unused2;
			RegisterType(type, out unused1, out unused2);
		}

		[Pure]
		private Type PatchType(Type type)
		{
			if (type.Is(typeof (Type)))
				return typeof (Type);

			return type;
		}

		private void RegisterType(Type type, out WriteMethod writeMethod, out ReadMethod readMethod)
		{
			type = PatchType(type);
			TypeInformation typeInfo = null;
			if (!_typeToWriteMethods.TryGetValue(type, out writeMethod))
			{
				typeInfo = new TypeInformation(type);
				writeMethod = CompileWriteMethod(typeInfo);
			}

			if (!_typeToReadMethods.TryGetValue(type, out readMethod))
			{
				if (typeInfo == null)
					typeInfo = new TypeInformation(type);

				readMethod = CompileReadMethod(typeInfo);
			}
		}

		public void WriteObject(BinaryWriter writer, object value)
		{
			if (value == null)
			{
				writer.Write("null");
			}
			else
			{
				var type = value.GetType();
				var fn = GetWriteObjectDelegate(type);
				fn(writer, value, this);
			}
		}

		public object ReadObject(BinaryReader reader)
		{
			var typeName = reader.ReadString();
			if (typeName != "null")
			{
				var type = Type.GetType(typeName);
				var fn = GetReadObjectDelegate(type);
				return fn(reader, this);
			}

			return null;
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

			return _typeToReadMethods.ContainsKey(type);
		}
	}
}