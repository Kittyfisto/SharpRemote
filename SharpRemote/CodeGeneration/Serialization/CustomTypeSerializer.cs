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
			if (type.GetCustomAttribute<DataContractAttribute>() == null)
				throw new ArgumentException(
					string.Format("The type '{0}.{1}' is missing the [DataContract] attribute, nor is there a custom-serializer available for this type", type.Namespace,
						type.Name));

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
					null,
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

			Action loadWriter = () => gen.Emit(OpCodes.Ldarg_0);
			Action loadSerializer = () => gen.Emit(OpCodes.Ldarg_2);
			Action loadValue = () =>
			{
				if (type.IsValueType)
				{
					gen.Emit(OpCodes.Ldarga, 1);
				}
				else
				{
					gen.Emit(OpCodes.Ldarg_1);
				}
			};

			foreach (var field in allFields)
			{
				Action loadField = () =>
				{
					loadValue();
					gen.Emit(OpCodes.Ldfld, field);
				};
				Action loadFieldAddress = () =>
				{
					loadValue();
					gen.Emit(OpCodes.Ldflda, field);
				};

				EmitWriteValue(gen,
					loadWriter,
					loadField,
					loadFieldAddress,
					loadSerializer,
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
		/// <param name="loadValueAddress"></param>
		/// <param name="loadSerializer"></param>
		/// <param name="valueType"></param>
		public void EmitWriteValue(ILGenerator gen,
			Action loadWriter,
			Action loadValue,
			Action loadValueAddress,
			Action loadSerializer,
			Type valueType)
		{
			// For now, let's inline everything that the Methods class can write and everything
			// else is delegated through a method...
			if (EmitWriteNativeType(gen,
				loadWriter,
				loadValue,
				loadValueAddress,
				valueType))
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
			if (EmitReadNativeType(gen, loadReader, valueType))
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
			if (typeInformation.IsValueType)
			{
				gen.Emit(OpCodes.Ldloca, tmp);
				gen.Emit(OpCodes.Initobj, typeInformation.Type);
			}
			else
			{
				var ctor = typeInformation.Type.GetConstructor(new Type[0]);
				if (ctor == null)
					throw new ArgumentException(string.Format("Type '{0}' is missing a parameterless constructor", typeInformation.Type));

				gen.Emit(OpCodes.Newobj, ctor);
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