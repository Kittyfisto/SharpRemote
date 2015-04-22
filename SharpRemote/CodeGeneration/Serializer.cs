using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace SharpRemote.CodeGeneration
{
	public sealed class Serializer
		: ISerializer
	{
		private readonly ModuleBuilder _module;
		private readonly Dictionary<Type, WriteMethod> _typeToWriteMethods;
		private readonly Dictionary<Type, ReadMethod> _typeToReadMethods;

		sealed class ReadMethod
		{
			public readonly MethodInfo Info;
			public Func<BinaryReader, ISerializer, object> ReadDelegate;

			public ReadMethod(MethodBuilder method)
			{
				Info = method;
			}
		}

		sealed class WriteMethod
		{
			public readonly MethodInfo Info;
			public Action<BinaryWriter, object, ISerializer> WriteDelegate;

			public WriteMethod(MethodBuilder method)
			{
				Info = method;
			}
		}

		public Serializer(ModuleBuilder module)
		{
			if (module == null) throw new ArgumentNullException("module");

			_module = module;
			_typeToWriteMethods = new Dictionary<Type, WriteMethod>();
			_typeToReadMethods = new Dictionary<Type, ReadMethod>();
		}

		/// <summary>
		/// Writes the current value on top of the evaluation stack onto the binary writer that's
		/// second to top on the evaluation stack.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="valueType"></param>
		/// <param name="serializer"></param>
		public void WriteValue(ILGenerator gen, Type valueType, FieldBuilder serializer)
		{
			if (!gen.EmitWritePodToWriter(valueType))
			{
				var writeObject = GetWriteObjectMethodInfo(valueType);

				//gen.EmitWriteLine("Pre-WriteObject()");

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, serializer);

				gen.Emit(OpCodes.Call, writeObject);
			}
		}

		/// <summary>
		///     Returns the method to write a value of the given type to a writer.
		///     Signature: WriteSealed(ISerializer serializer, BinaryWriter writer, object value)
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public MethodInfo GetWriteObjectMethodInfo(Type type)
		{
			WriteMethod method;
			if (!_typeToWriteMethods.TryGetValue(type, out method))
			{
				method = CompileWriteMethod(type);
			}
			return method.Info;
		}

		private Action<BinaryWriter, object, ISerializer> GetWriteObjectDelegate(Type type)
		{
			WriteMethod method;
			if (!_typeToWriteMethods.TryGetValue(type, out method))
			{
				method = CompileWriteMethod(type);
			}
			return method.WriteDelegate;
		}

		private Func<BinaryReader, ISerializer, object> GetReadObjectDelegate(Type type)
		{
			ReadMethod method;
			if (!_typeToReadMethods.TryGetValue(type, out method))
			{
				method = CompileReadMethod(type);
			}
			return method.ReadDelegate;
		}

		[Pure]
		private static bool CanBeSerialized(Type type)
		{
			if (type.IsPrimitive)
				return true;

			if (type.GetCustomAttribute<DataContractAttribute>() != null)
				return true;

			return false;
		}

		/// <summary>
		/// Whether or not a value of the given type *always* requires type information
		/// to be injected into the bytestream.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private static bool RequiresTypeInformation(Type type)
		{
			if (type.IsPrimitive)
				return false;

			if (type.IsValueType)
				return false;

			if (type.IsSealed)
				return false;

			return true;
		}

		private ReadMethod CompileReadMethod(Type type)
		{
			if (!CanBeSerialized(type))
				throw new ArgumentException(string.Format("Type '{0}' is missing the DataContract attribute", type));

			var typeName = string.Format("Read.{0}.{1}", type.Namespace, type.Name);
			var typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			var method = typeBuilder.DefineMethod("ReadValue", MethodAttributes.Public | MethodAttributes.Static,
												   CallingConventions.Standard, type, new[]
				                                       {
														   typeof(BinaryReader),
														   typeof (ISerializer)
				                                       });

			CreateReadDelegate(typeBuilder, method, type);
			var m = new ReadMethod(method);
			_typeToReadMethods.Add(type, m);

			var gen = method.GetILGenerator();

			if (type.IsPrimitive)
			{
				gen.Emit(OpCodes.Ldarg_0);

				if (!gen.EmitReadPod(type))
					throw new NotImplementedException();
			}
			else
			{
				throw new NotImplementedException();
			}

			gen.Emit(OpCodes.Ret);

			var serializerType = typeBuilder.CreateType();
			var delegateMethod = serializerType.GetMethod("ReadObject", new[] { typeof(BinaryReader), typeof(ISerializer) });
			m.ReadDelegate = (Func<BinaryReader, ISerializer, object>)delegateMethod.CreateDelegate(typeof(Func<BinaryReader, ISerializer, object>));

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

			gen.Emit(OpCodes.Ret);
		}

		private WriteMethod CompileWriteMethod(Type type)
		{
			if (!CanBeSerialized(type))
				throw new ArgumentException(string.Format("Type '{0}' is missing the DataContract attribute", type));

			var typeName = string.Format("Write.{0}.{1}", type.Namespace, type.Name);
			var typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			var method = typeBuilder.DefineMethod("WriteValue", MethodAttributes.Public | MethodAttributes.Static,
			                                       CallingConventions.Standard, typeof (void), new[]
				                                       {
														   typeof(BinaryWriter),
					                                       type,
														   typeof (ISerializer)
				                                       });
			CreateWriteDelegate(typeBuilder, method, type);
			var m = new WriteMethod(method);
			_typeToWriteMethods.Add(type, m);

			var gen = method.GetILGenerator();

			if (type.IsPrimitive)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				if (!gen.EmitWritePodToWriter(type))
					throw new NotImplementedException();
			}
			else if (type.IsValueType)
			{
				WriteFields(gen, type);
			}
			else if (type.IsSealed)
			{
				WriteSealedObject(gen, type);
			}
			else
			{
				WriteUnsealedObject(gen, type);
			}

			var serializerType = typeBuilder.CreateType();
			var delegateMethod = serializerType.GetMethod("WriteObject", new[] { typeof(BinaryWriter), typeof(object), typeof(ISerializer) });
			m.WriteDelegate = (Action<BinaryWriter, object, ISerializer>)delegateMethod.CreateDelegate(typeof(Action<BinaryWriter, object, ISerializer>));

			return m;
		}

		private void CreateWriteDelegate(TypeBuilder typeBuilder, MethodInfo methodInfo, Type type)
		{
			var method = typeBuilder.DefineMethod("WriteObject", MethodAttributes.Public | MethodAttributes.Static,
												   CallingConventions.Standard, typeof(void), new[]
				                                       {
														   typeof(BinaryWriter),
					                                       typeof(object),
														   typeof (ISerializer)
				                                       });
			var gen = method.GetILGenerator();

			if (!RequiresTypeInformation(type))
			{
				// We need to inject type information here...
				WriteTypeInformationOrNull(gen, type, () =>
					{
						gen.Emit(OpCodes.Ldarg_0);
						gen.Emit(OpCodes.Ldarg_1);

						if (type.IsPrimitive)
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
			}
			else
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Castclass, type);
				gen.Emit(OpCodes.Ldarg_2);
				gen.Emit(OpCodes.Call, methodInfo);
			}
		}

		private void WriteTypeInformationOrNull(ILGenerator gen, Type type, Action emitSerializationCode)
		{
			//gen.EmitWriteLine("writing type info");

			var result = gen.DeclareLocal(typeof(int));

			// if (object == null)
			var @true = gen.DefineLabel();
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc, result);
			gen.Emit(OpCodes.Ldloc, result);
			gen.Emit(OpCodes.Brtrue_S, @true);

			// { writer.WriteString(string.Empty); }
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldsfld, Methods.StringEmpty);
			gen.Emit(OpCodes.Callvirt, Methods.WriteString);
			var @end = gen.DefineLabel();
			gen.Emit(OpCodes.Br_S, @end);

			// else { writer.WriteString(object.GetType().AssemblyQualifiedName);
			gen.MarkLabel(@true);
			//gen.EmitWriteLine("writer.WriteString(object.GetType().AssemblyQualifiedName)");
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, Methods.ObjectGetType);
			gen.Emit(OpCodes.Callvirt, Methods.TypeGetAssemblyQualifiedName);
			gen.Emit(OpCodes.Callvirt, Methods.WriteString);

			emitSerializationCode();

			gen.MarkLabel(@end);
			gen.Emit(OpCodes.Ret);

			//gen.EmitWriteLine("Type info written");
		}

		private void WriteUnsealedObject(ILGenerator gen, Type type)
		{
			WriteTypeInformationOrNull(gen, type, () =>
				{
					// _serializer.WriteObject(writer, object); }
					WriteFields(gen, type);
				});
		}

		/// <summary>
		/// Write an object who's property-type is sealed (and thus the final type is known at compile time).
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void WriteSealedObject(ILGenerator gen, Type type)
		{
			var result = gen.DeclareLocal(typeof(int));

			// if (object == null)
			var @true = gen.DefineLabel();
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc, result);
			gen.Emit(OpCodes.Ldloc, result);
			gen.Emit(OpCodes.Brtrue_S, @true);

			// { writer.Write(false); }
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Call, Methods.WriteBool);

			var @end = gen.DefineLabel();
			gen.Emit(OpCodes.Br_S, @end);

			// else { writer.Write(true); <Serialize Fields> }
			gen.MarkLabel(@true);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Call, Methods.WriteBool);
			WriteFields(gen, type);

			gen.MarkLabel(@end);
			gen.Emit(OpCodes.Ret);
		}

		private void WriteFields(ILGenerator gen, Type type)
		{
			var allFields =
				type.GetFields(BindingFlags.Public | BindingFlags.Instance)
				    .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
				    .ToArray();

			foreach (var field in allFields)
			{
				gen.Emit(OpCodes.Ldarg_0);

				if (type.IsValueType)
				{
					gen.Emit(OpCodes.Ldarga, 1);
				}
				else
				{
					gen.Emit(OpCodes.Ldarg_1);
				}
				gen.Emit(OpCodes.Ldfld, field);

				WriteValue(gen, field.FieldType, null);
			}
		}

		public void RegisterType<T>()
		{
			var type = typeof (T);
			if (!_typeToWriteMethods.ContainsKey(type))
			{
				CompileWriteMethod(type);
				CompileReadMethod(type);
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
	}
}