using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace SharpRemote.CodeGeneration
{
	public sealed class SerializerCompiler
		: ISerializer
	{
		private readonly ModuleBuilder _module;
		private readonly Dictionary<Type, Method> _typeToWriteMethod;

		sealed class Method
		{
			public readonly MethodInfo Info;
			public Action<BinaryWriter, object, ISerializer> WriteDelegate;

			public Method(MethodBuilder method)
			{
				Info = method;
			}
		}

		public SerializerCompiler(ModuleBuilder module)
		{
			if (module == null) throw new ArgumentNullException("module");

			_module = module;
			_typeToWriteMethod = new Dictionary<Type, Method>();
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
				var writeObject = WriteObject(valueType);

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
		public MethodInfo WriteObject(Type type)
		{
			Method method;
			if (!_typeToWriteMethod.TryGetValue(type, out method))
			{
				method = CompileWriteMethod(type);
			}
			return method.Info;
		}

		private Action<BinaryWriter, object, ISerializer> GetWriteMethod(Type type)
		{
			Method method;
			if (!_typeToWriteMethod.TryGetValue(type, out method))
			{
				method = CompileWriteMethod(type);
			}
			return method.WriteDelegate;
		}

		private Method CompileWriteMethod(Type type)
		{
			if (type.GetCustomAttribute<DataContractAttribute>() == null)
				throw new ArgumentException(string.Format("Type '{0}' is missing the DataContract attribute", type));

			var typeName = string.Format("{0}.{1}.Serializer", type.Namespace, type.Name);
			var typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			var method = typeBuilder.DefineMethod("WriteObject", MethodAttributes.Public | MethodAttributes.Static,
			                                       CallingConventions.Standard, typeof (void), new[]
				                                       {
														   typeof(BinaryWriter),
					                                       type,
														   typeof (ISerializer)
				                                       });
			CreateWriteDelegate(typeBuilder, method, type);
			var m = new Method(method);
			_typeToWriteMethod.Add(type, m);

			var gen = method.GetILGenerator();

			if (type.IsValueType)
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
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, type);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Call, methodInfo);
		}

		private void WriteUnsealedObject(ILGenerator gen, Type type)
		{
			//gen.EmitWriteLine("writing type info");

			var result = gen.DeclareLocal(typeof (int));

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

			// _serializer.WriteObject(writer, object); }
			WriteFields(gen, type);

			gen.MarkLabel(@end);
			gen.Emit(OpCodes.Ret);

			//gen.EmitWriteLine("Type info written");
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

		public void WriteObject(BinaryWriter writer, object value)
		{
			if (value == null)
			{
				writer.Write("null");
			}
			else
			{
				var type = value.GetType();
				var fn = GetWriteMethod(type);
				fn(writer, value, this);
			}
		}

		public object Deserialize(BinaryReader reader, Type type)
		{
			throw new NotImplementedException();
		}
	}
}