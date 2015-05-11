using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace SharpRemote.CodeGeneration.Serialization
{
	public partial class Serializer
	{
		#region Writing

		/// <summary>
		///     Write an object who's compile-time-type is not sealed and thus the instance-type could differ from the compile-time type,
		///     requring us to *always* emit type information into the stream.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void WriteUnsealedObject(ILGenerator gen, Type type)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///     Write an object who's compile-time-type is sealed (and thus the actual type of each instance is known at compile time).
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void WriteSealedObject(ILGenerator gen, Type type)
		{
			LocalBuilder result = gen.DeclareLocal(typeof (int));

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
			EmitWriteAllFieldsAndProperties(gen, type);

			gen.MarkLabel(@end);
			gen.Emit(OpCodes.Ret);
		}

		/// <summary>
		///     Emits code to write all properties of the given type into a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void EmitWriteProperties(ILGenerator gen, Type type)
		{
			PropertyInfo[] allProperties =
				type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				    .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
				    .ToArray();

			foreach (var property in allProperties)
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

						gen.Emit(OpCodes.Call, property.GetMethod);
					}, property.PropertyType))
				{
					MethodInfo writeObject = GetWriteValueMethodInfo(property.PropertyType);

					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldarg_2);

					gen.Emit(OpCodes.Call, writeObject);
				}
			}
		}

		/// <summary>
		///     Emits code to write all fields of the given type into a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void EmitWriteFields(ILGenerator gen, Type type)
		{
			FieldInfo[] allFields =
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
					MethodInfo writeObject = GetWriteValueMethodInfo(field.FieldType);

					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldarg_2);

					gen.Emit(OpCodes.Call, writeObject);
				}
			}
		}

		/// <summary>
		///     Emits code to write all fields and properties of the given type into a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void EmitWriteAllFieldsAndProperties(ILGenerator gen, Type type)
		{
			EmitWriteFields(gen, type);
			EmitWriteProperties(gen, type);
		}

		#endregion

		#region Reading

		private void EmitReadValueType(ILGenerator gen, TypeInformation type, LocalBuilder local)
		{
			CreateAndStoreNewInstance(gen, type, local);
			EmitReadAllFieldsAndProperties(gen, type, local);
			gen.Emit(OpCodes.Ldloc, local);
		}

		private void EmitReadSealedClass(ILGenerator gen, TypeInformation type, LocalBuilder local)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ReadBool);
			var end = gen.DefineLabel();
			var @null = gen.DefineLabel();
			gen.Emit(OpCodes.Brfalse, @null);

			CreateAndStoreNewInstance(gen, type, local);
			EmitReadAllFieldsAndProperties(gen, type, local);
			gen.Emit(OpCodes.Ldloc, local);
			gen.Emit(OpCodes.Br_S, end);

			gen.MarkLabel(@null);
			gen.Emit(OpCodes.Ldnull);

			gen.MarkLabel(end);
		}

		private void EmitReadUnsealedClass(ILGenerator gen, TypeInformation type)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a new instance of the given type and stores it in the given local variable.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="typeInformation"></param>
		/// <param name="tmp"></param>
		private static void CreateAndStoreNewInstance(ILGenerator gen, TypeInformation typeInformation, LocalBuilder tmp)
		{
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
		}

		private void EmitReadAllFieldsAndProperties(ILGenerator gen, TypeInformation type, LocalBuilder target)
		{
			EmitReadAllFields(gen, type, target);
			EmitReadAllProperties(gen, type, target);
		}

		private void EmitReadAllProperties(ILGenerator gen, TypeInformation type, LocalBuilder target)
		{
			PropertyInfo[] allProperties = type.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			                                   .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
			                                   .ToArray();

			foreach (var property in allProperties)
			{
				gen.Emit(type.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc, target);
				Type propertyType = property.PropertyType;
				if (!gen.EmitReadNativeType(() => gen.Emit(OpCodes.Ldarg_0), propertyType))
				{
					throw new NotImplementedException();
				}
				gen.Emit(OpCodes.Call, property.SetMethod);
			}
		}

		private void EmitReadAllFields(ILGenerator gen, TypeInformation type, LocalBuilder target)
		{
			FieldInfo[] allFields = type.Type.GetFields(BindingFlags.Public | BindingFlags.Instance)
			                         .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
			                         .ToArray();

			foreach (var field in allFields)
			{
				// tmp.<Field> = writer.ReadXYZ();
				gen.Emit(type.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc, target);

				Type fieldType = field.FieldType;
				if (!gen.EmitReadNativeType(() => gen.Emit(OpCodes.Ldarg_0), fieldType))
				{
					throw new NotImplementedException();
				}

				gen.Emit(OpCodes.Stfld, field);
			}
		}

		#endregion

		private void WriteValueType(ILGenerator gen, TypeInformation typeInformation)
		{
			EmitWriteAllFieldsAndProperties(gen, typeInformation.Type);
		}
	}
}