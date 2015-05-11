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
		///     Emits the code to write an object of the given type to a <see cref="BinaryWriter"/>.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void WriteCustomType(ILGenerator gen, Type type)
		{
			EmitWriteFields(gen, type);
			EmitWriteProperties(gen, type);
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
				EmitWriteValue(gen,
					() => gen.Emit(OpCodes.Ldarg_0),
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
					},
					() => gen.Emit(OpCodes.Ldarg_2),
					property.PropertyType
					);
			}
		}

		/// <summary>
		///     Emits code to write all fields of the given type into a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		public void EmitWriteFields(ILGenerator gen, Type type)
		{
			FieldInfo[] allFields =
				type.GetFields(BindingFlags.Public | BindingFlags.Instance)
				    .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
				    .ToArray();

			foreach (var field in allFields)
			{
				EmitWriteValue(gen,
					() => gen.Emit(OpCodes.Ldarg_0),
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
					},
					() => gen.Emit(OpCodes.Ldarg_2),
					field.FieldType
					);
			}
		}

		/// <summary>
		/// Emits the code necessary to write a value of the given compile-time type to
		/// a <see cref="BinaryWriter"/>.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadWriter"></param>
		/// <param name="loadValue"></param>
		/// <param name="loadSerializer"></param>
		/// <param name="valueType"></param>
		public void EmitWriteValue(ILGenerator gen,
			Action loadWriter,
			Action loadValue,
			Action loadSerializer,
			Type valueType)
		{
			// For now, let's inline everything that the Methods class can write and everything
			// else is delegated through a method...
			if (gen.EmitWriteNativeType(loadWriter, loadValue, valueType))
			{
				// Nothing to do...
			}
			else
			{
				var method = GetWriteValueMethodInfo(valueType);
				if (method.IsStatic) //< Signature: void Write(BinaryWriter, T, ISerializer)
				{
					loadWriter();
					loadValue();
					loadSerializer();
					gen.Emit(OpCodes.Call, method);
				}
				else //< Signature: ISerializer.WriteObject(BinaryWriter, object)
				{
					loadSerializer();
					loadWriter();
					loadValue();
					gen.Emit(OpCodes.Callvirt, method);
				}
			}
		}

		#endregion

		#region Reading

		public void EmitReadValue(ILGenerator gen,
			Action loadReader,
			Action loadSerializer,
			Type valueType)
		{
			if (gen.EmitReadNativeType(loadReader, valueType))
			{
				// Nothing to do...
			}
			else
			{
				var method = GetReadValueMethodInfo(valueType);
				if (method.IsStatic) //< Signature: T Read(BinaryReader, ISerializer
				{
					loadReader();
					loadSerializer();
					gen.Emit(OpCodes.Call, method);
				}
				else //< Signature: T ISerializer.Read(BinaryReader)
				{
					loadSerializer();
					loadReader();
					gen.Emit(OpCodes.Callvirt, method);
				}
			}
		}

		private void EmitReadCustomType(ILGenerator gen, TypeInformation type, LocalBuilder target)
		{
			CreateAndStoreNewInstance(gen, type, target);
			EmitReadAllFields(gen, type, target);
			EmitReadAllProperties(gen, type, target);
			gen.Emit(OpCodes.Ldloc, target);
		}

		/// <summary>
		/// Creates a new instance of the given type and stores it in the given target variable.
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

		private void EmitReadAllProperties(ILGenerator gen, TypeInformation type, LocalBuilder target)
		{
			PropertyInfo[] allProperties = type.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			                                   .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
			                                   .ToArray();

			foreach (var property in allProperties)
			{
				gen.Emit(type.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc, target);
				Type propertyType = property.PropertyType;
				EmitReadValue(gen,
					() => gen.Emit(OpCodes.Ldarg_0),
					() => gen.Emit(OpCodes.Ldarg_1),
					propertyType);

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
				EmitReadValue(gen,
					() => gen.Emit(OpCodes.Ldarg_0),
					() => gen.Emit(OpCodes.Ldarg_1),
					fieldType);

				gen.Emit(OpCodes.Stfld, field);
			}
		}

		#endregion
	}
}