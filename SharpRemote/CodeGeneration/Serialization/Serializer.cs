using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace SharpRemote.CodeGeneration.Serialization
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

		public Serializer()
		{
			var assemblyName = new AssemblyName("SharpRemote.CodeGeneration.Serializer");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);

			_module = module;
			_typeToWriteMethods = new Dictionary<Type, WriteMethod>();
			_typeToReadMethods = new Dictionary<Type, ReadMethod>();
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
		/// <param name="loadWriter"></param>
		/// <param name="loadValue"></param>
		/// <param name="valueType"></param>
		/// <param name="serializer"></param>
		public void WriteValue(ILGenerator gen,
			Action loadWriter,
			Action loadValue,
			Type valueType,
			FieldBuilder serializer)
		{
			if (!gen.EmitWritePod(loadWriter, loadValue, valueType))
			{
				var writeObject = GetWriteObjectMethodInfo(valueType);

				// Serializer.Serialize(writer, value, this.serializer)
				loadWriter();
				loadValue();
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
			if (type.IsValueType || type.IsSealed)
			{
				WriteMethod method;
				ReadMethod unused;
				RegisterType(type, out method, out unused);
				return method.ValueMethod;
			}

			// We don't know the true type of the parameter until we inspect it's actual value.
			// Thus we're forced to do a dynamic dispatch.
			throw new NotImplementedException();
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

			return method.ReadDelegate;
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
				ReadArray(gen, typeInformation);
			}
			else if (gen.EmitReadPod(() => gen.Emit(OpCodes.Ldarg_0),
					typeInformation.Type))
			{
				
			}
			else if (typeInformation.IsArray)
			{
				
			}
			else
			{
				var tmp = gen.DeclareLocal(typeInformation.Type);
				if (typeInformation.Constructor == null)
				{
					gen.Emit(OpCodes.Ldloca, tmp);
					gen.Emit(OpCodes.Initobj, typeInformation.Type);
				}
				else
				{
					gen.Emit(OpCodes.Newobj, typeInformation.Constructor);
					gen.Emit(OpCodes.Stloc, tmp);
				}

				ReadFields(gen, tmp, typeInformation.Type);

				gen.Emit(OpCodes.Ldloc, tmp);
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
			else
			{
				gen.Emit(OpCodes.Castclass, type);
			}

			gen.Emit(OpCodes.Ret);
		}

		private void ReadFields(ILGenerator gen, LocalBuilder target, Type type)
		{
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
			                 .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
			                 .ToArray();

			foreach (var field in fields)
			{
				ReadField(gen, target, field);
			}
		}

		private void ReadField(ILGenerator gen, LocalBuilder target, FieldInfo field)
		{
			// tmp.<Field> = writer.ReadXYZ();

			gen.Emit(OpCodes.Ldloca, target);

			var type = field.FieldType;
			if (!gen.EmitReadPod(() => gen.Emit(OpCodes.Ldarg_0), type))
			{
				throw new NotImplementedException();
			}

			gen.Emit(OpCodes.Stfld, field);
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
				WriteArray(gen, typeInformation);
			}
			else if (gen.EmitWritePod(
				() => gen.Emit(OpCodes.Ldarg_0),
				() => gen.Emit(OpCodes.Ldarg_1),
				typeInformation.Type))
			{
				gen.Emit(OpCodes.Ret);
			}
			else if (typeInformation.IsValueType)
			{
				WriteFields(gen, typeInformation.Type);
				gen.Emit(OpCodes.Ret);
			}
			else if (typeInformation.IsSealed)
			{
				WriteSealedObject(gen, typeInformation.Type);
			}
			else
			{
				WriteUnsealedObject(gen, typeInformation.Type);
			}

			var serializerType = typeBuilder.CreateType();
			var delegateMethod = serializerType.GetMethod("WriteObject", new[] { typeof(BinaryWriter), typeof(object), typeof(ISerializer) });
			m.WriteDelegate = (Action<BinaryWriter, object, ISerializer>)delegateMethod.CreateDelegate(typeof(Action<BinaryWriter, object, ISerializer>));

			return m;
		}

		private void ReadArray(ILGenerator gen, TypeInformation typeInformation)
		{
			var elementType = typeInformation.ElementType;

			var value = gen.DeclareLocal(typeInformation.Type);
			var count = gen.DeclareLocal(typeof (int));
			var i = gen.DeclareLocal(typeof (int));

			// count = reader.ReadInt32()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ReadInt);
			gen.Emit(OpCodes.Stloc, count);

			// value = new XXX[count]
			gen.Emit(OpCodes.Ldloc, count);
			gen.Emit(OpCodes.Newarr, elementType);
			gen.Emit(OpCodes.Stloc, value);

			var loop = gen.DefineLabel();
			var end = gen.DefineLabel();

			// int i = 0
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Stloc, i);

			// loop:
			gen.MarkLabel(loop);
			// if (i < count) goto end
			gen.Emit(OpCodes.Ldloc, i);
			gen.Emit(OpCodes.Ldloc, count);
			gen.Emit(OpCodes.Clt);
			gen.Emit(OpCodes.Brfalse, end);

			// value[i] = <ReadValue>
			gen.Emit(OpCodes.Ldloc, value);
			gen.Emit(OpCodes.Ldloc, i);
			if (gen.EmitReadPod(
				() => gen.Emit(OpCodes.Ldarg_0),
				elementType
				))
			{
				
			}
			else
			{
				throw new NotImplementedException();
			}

			gen.Emit(OpCodes.Stelem, elementType);

			// ++i
			gen.Emit(OpCodes.Ldloc, i);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Add);
			gen.Emit(OpCodes.Stloc, i);
			// goto loop
			gen.Emit(OpCodes.Br, loop);

			// end:
			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ldloc, value);
		}

		private void WriteArray(ILGenerator gen, TypeInformation typeInformation)
		{
			var type = typeInformation.Type;
			var elementType = typeInformation.ElementType;
			var enumerableType = typeof (IEnumerable<>).MakeGenericType(elementType);
			var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);
			var getLength = type.GetProperty("Length").GetMethod;
			var getEnumerator = enumerableType.GetMethod("GetEnumerator");
			var moveNext = typeof(IEnumerator).GetMethod("MoveNext");
			var getCurrent = enumeratorType.GetProperty("Current").GetMethod;

			// writer.Write(value.Length) OR writer.Write(value.Count)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, getLength);
			gen.Emit(OpCodes.Call, Methods.WriteInt);

			// var enumerator = value.GetEnumerator()
			var enumerator = gen.DeclareLocal(enumeratorType);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, enumerableType);
			gen.Emit(OpCodes.Callvirt, getEnumerator);
			gen.Emit(OpCodes.Stloc, enumerator);

			var loop = gen.DefineLabel();
			var end = gen.DefineLabel();

			// loop:
			gen.MarkLabel(loop);
			// if (!enumerator.MoveNext()) goto end
			gen.Emit(OpCodes.Ldloc, enumerator);
			gen.Emit(OpCodes.Callvirt, moveNext);
			gen.Emit(OpCodes.Brfalse, end);

			if (gen.EmitWritePod(
				() => gen.Emit(OpCodes.Ldarg_0),
				() =>
					{
						gen.Emit(OpCodes.Ldloc, enumerator);
						gen.Emit(OpCodes.Callvirt, getCurrent);
					},
				elementType))
			{
				
			}
			else if (elementType.IsValueType)
			{
				WriteFields(gen, elementType);
			}
			else
			{
				throw new NotImplementedException();
			}

			// goto loop
			gen.Emit(OpCodes.Br, loop);

			// end:
			gen.MarkLabel(end);
			// return
			gen.Emit(OpCodes.Ret);
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

			WriteTypeInformationOrNull(gen, type, () =>
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

		private void WriteTypeInformation(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, Methods.ObjectGetType);
			gen.Emit(OpCodes.Callvirt, Methods.TypeGetAssemblyQualifiedName);
			gen.Emit(OpCodes.Callvirt, Methods.WriteString);
		}

		private void WriteTypeInformationOrNull(ILGenerator gen, Type type, Action emitSerializationCode)
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
			WriteTypeInformation(gen);

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
			gen.Emit(OpCodes.Brtrue, @true);

			// { writer.Write(false); }
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Call, Methods.WriteBool);

			var @end = gen.DefineLabel();
			gen.Emit(OpCodes.Br, @end);

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
				if (!gen.EmitWritePod(() => gen.Emit(OpCodes.Ldarg_0),
				                              () =>
					                              {
						                              if (type.IsValueType)
						                              {
							                              gen.Emit(OpCodes.Ldarga, 1);
						                              }
						                              else
						                              {
							                              gen.Emit(OpCodes.Ldarg_1);
						                              }
						                              gen.Emit(OpCodes.Ldfld, field);
					                              }, field.FieldType))
				{
					var writeObject = GetWriteObjectMethodInfo(field.FieldType);

					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldarg_2);

					gen.Emit(OpCodes.Call, writeObject);
				}
			}
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