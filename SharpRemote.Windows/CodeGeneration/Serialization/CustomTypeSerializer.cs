using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using SharpRemote.CodeGeneration;
using SerializationException = SharpRemote.Exceptions.SerializationException;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public partial class Serializer
	{
		#region Writing

		/// <summary>
		///     Emits the code to write an object of the given type to a <see cref="BinaryWriter"/>.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		/// <param name="loadWriter"></param>
		/// <param name="loadRemotingEndPoint"></param>
		private void WriteCustomType(ILGenerator gen,
			Type type,
			Action loadWriter,
			Action loadRemotingEndPoint)
		{
			if (type.GetCustomAttribute<DataContractAttribute>() != null)
			{
				// The type should be serialized by value, e.g. we simply serialize all fields and properties
				// marked with the [DataMember] attribute
				EmitWriteFields(gen, loadRemotingEndPoint, type);
				EmitWriteProperties(gen, loadRemotingEndPoint, type);
			}
			else if (type.GetRealCustomAttribute<ByReferenceAttribute>(true) != null)
			{
				// The type should be serialized by reference, e.g. we create a servant for it
				// (or retrieve an existing one) and then write its (unique) object id to the stream.

				var proxyInterface = FindProxyInterface(type);

				var method = typeof(IRemotingEndPoint).GetMethod("GetExistingOrCreateNewServant").MakeGenericMethod(proxyInterface);

				// writer.Write(_remotingEndPoint.GetExistingOrCreateNewServant<T>(value).ObjectId);
				loadWriter();
				loadRemotingEndPoint();
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Callvirt, method);
				gen.Emit(OpCodes.Callvirt, Methods.GrainGetObjectId);
				gen.Emit(OpCodes.Call, Methods.WriteLong);
			}
			else
			{
				throw new ArgumentException(
					string.Format("The type '{0}.{1}' is missing the [DataContract] or [ByReference] attribute, nor is there a custom-serializer available for this type", type.Namespace,
						type.Name));
			}
		}

		private static Type FindProxyInterface(Type type)
		{
			if (type.GetCustomAttribute<DataContractAttribute>(true) != null)
				throw new ArgumentException(string.Format("The type '{0}' is marked with the [DataContract] as well as [ByReference] attribute, but these are mutually exclusive",
					type.FullName));

			Type proxyInterface;
			var attributed = type.GetInterfaces().Where(x => x.GetCustomAttribute<ByReferenceAttribute>() != null).ToList();
			if (attributed.Count > 1)
				throw new ArgumentException(
					string.Format(
						"Currently a type may implement only one interface marked with the [ByReference] attribute, but '{0}' implements more than that: {1}",
						type.FullName,
						string.Join(", ", attributed.Select(x => x.FullName))
						)
					);

			if (attributed.Count == 0)
			{
				if (type.GetCustomAttribute<ByReferenceAttribute>(false) == null)
					throw new SystemException(string.Format("Unable to extract the correct proxy interface for type '{0}'",
					                                        type.FullName));

				proxyInterface = type;
			}
			else
			{
				proxyInterface = attributed[0];
			}
			return proxyInterface;
		}

		/// <summary>
		///     Emits code to write all properties of the given type into a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadRemotingEndPoint"></param>
		/// <param name="type"></param>
		private void EmitWriteProperties(ILGenerator gen,
			Action loadRemotingEndPoint,
			Type type)
		{
			PropertyInfo[] allProperties =
				type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				    .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
				    .ToArray();

			foreach (var property in allProperties)
			{
				Action loadWriter = () => gen.Emit(OpCodes.Ldarg_0);
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

						gen.Emit(OpCodes.Call, property.GetMethod);
					};

				Action loadValueAddress;
				if (property.PropertyType.IsValueType)
					loadValueAddress = () =>
						{
							var loc = gen.DeclareLocal(property.PropertyType);
							loadValue();
							gen.Emit(OpCodes.Stloc, loc);
							gen.Emit(OpCodes.Ldloca, loc);
						};
				else loadValueAddress = null;

				try
				{
					EmitWriteValue(gen,
						loadWriter,
						loadValue,
						loadValueAddress,
						() => gen.Emit(OpCodes.Ldarg_2),
						loadRemotingEndPoint,
						property.PropertyType
						);
				}
				catch (SerializationException)
				{
					throw;
				}
				catch (Exception e)
				{
					var message = string.Format("There was a problem generating the code to serialize property '{0} {1}' of type '{2}' ",
						property.PropertyType,
						property.Name,
						type.FullName
						);
					throw new SerializationException(message, e);
				}
			}
		}

		/// <summary>
		///     Emits code to write all fields of the given type into a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadRemotingEndPoint"></param>
		/// <param name="type"></param>
		public void EmitWriteFields(ILGenerator gen,
			Action loadRemotingEndPoint,
			Type type)
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

				try
				{
					EmitWriteValue(gen,
					               loadWriter,
					               loadField,
					               loadFieldAddress,
					               loadSerializer,
								   loadRemotingEndPoint,
					               field.FieldType
						);
				}
				catch (SerializationException)
				{
					throw;
				}
				catch (Exception e)
				{
					var message = string.Format("There was a problem generating the code to serialize field '{0} {1}' of type '{2}' ",
						field.FieldType,
						field.Name,
						type.FullName
						);
					throw new SerializationException(message, e);
				}
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
		/// <param name="loadRemotingEndPoint"></param>
		/// <param name="valueType"></param>
		public void EmitWriteValue(ILGenerator gen,
			Action loadWriter,
			Action loadValue,
			Action loadValueAddress,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			Type valueType)
		{
			// For now, let's inline everything that the Methods class can write and everything
			// else is delegated through a method...
			if (EmitWriteNativeType(gen,
				loadWriter,
				loadValue,
				loadValueAddress,
				loadSerializer,
				loadRemotingEndPoint,
				valueType))
			{
				// Nothing to do...
			}
			else
			{
				var method = GetWriteValueMethodInfo(valueType);
				if (method.IsStatic) //< Signature: void Write(BinaryWriter, T, ISerializer, IRemotingEndPoint)
				{
					loadWriter();
					loadValue();
					loadSerializer();
					loadRemotingEndPoint();
					gen.Emit(OpCodes.Call, method);
				}
				else //< Signature: ISerializer.WriteObject(BinaryWriter, object, IRemotingEndPoint)
				{
					loadSerializer();
					loadWriter();
					loadValue();
					loadRemotingEndPoint();
					gen.Emit(OpCodes.Callvirt, method);
				}
			}
		}

		#endregion

		#region Reading

		/// <summary>
		/// Emits the code necessary to read a value of the given compile-time type from
		/// a <see cref="BinaryReader"/>.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadReader"></param>
		/// <param name="loadSerializer"></param>
		/// <param name="loadRemotingEndPoint"></param>
		/// <param name="valueType"></param>
		public void EmitReadValue(ILGenerator gen,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			Type valueType)
		{
			if (EmitReadNativeType(gen, loadReader, loadSerializer, loadRemotingEndPoint, valueType))
			{
				// Nothing to do...
			}
			else
			{
				var method = GetReadValueMethodInfo(valueType);
				if (method.IsStatic) //< Signature: T Read(BinaryReader, ISerializer, IRemotingEndPoint)
				{
					loadReader();
					loadSerializer();
					loadRemotingEndPoint();
					gen.Emit(OpCodes.Call, method);
				}
				else //< Signature: T ISerializer.Read(BinaryReader, IRemotingEndPoint)
				{
					loadSerializer();
					loadReader();
					loadRemotingEndPoint();
					gen.Emit(OpCodes.Callvirt, method);
				}
			}
		}

		private void EmitReadCustomType(ILGenerator gen,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			TypeInformation type,
			LocalBuilder target)
		{
			if (type.Type.GetCustomAttribute<DataContractAttribute>() != null)
			{
				CreateAndStoreNewInstance(gen, type, target);
				EmitReadAllFields(gen,
								  loadReader,
								  loadSerializer,
								  loadRemotingEndPoint,
								  type,
								  target);

				EmitReadAllProperties(gen,
					loadReader,
					loadSerializer,
					loadRemotingEndPoint,
					type,
					target);

				gen.Emit(OpCodes.Ldloc, target);
			}
			else if (type.Type.GetRealCustomAttribute<ByReferenceAttribute>(true) != null)
			{
				var proxyInterface = FindProxyInterface(type.Type);
				var method = typeof(IRemotingEndPoint).GetMethod("GetExistingOrCreateNewProxy").MakeGenericMethod(proxyInterface);

				// result = _remotingEndPoint.GetExistingOrCreateNewProxy<T>(serializer.ReadLong());
				loadRemotingEndPoint();
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadLong);
				gen.Emit(OpCodes.Callvirt, method);

				gen.Emit(OpCodes.Stloc, target);
				gen.Emit(OpCodes.Ldloc, target);
			}
			else
			{
				throw new NotImplementedException();
			}
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

		private void EmitReadAllProperties(ILGenerator gen,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			TypeInformation type,
			LocalBuilder target)
		{
			PropertyInfo[] allProperties = type.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			                                   .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
			                                   .ToArray();

			foreach (var property in allProperties)
			{
				gen.Emit(type.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc, target);
				Type propertyType = property.PropertyType;

				try
				{
					EmitReadValue(gen,
					              loadReader,
					              loadSerializer,
					              loadRemotingEndPoint,
					              propertyType);
				}
				catch (SerializationException)
				{
					throw;
				}
				catch (Exception e)
				{
					var message = string.Format("There was a problem generating the code to deserialize property '{0} {1}' of type '{2}' ",
						property.PropertyType,
						property.Name,
						type.Type.FullName
						);
					throw new SerializationException(message, e);
				}

				gen.Emit(OpCodes.Call, property.SetMethod);
			}
		}

		private void EmitReadAllFields(ILGenerator gen,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			TypeInformation type,
			LocalBuilder target)
		{
			FieldInfo[] allFields = type.Type.GetFields(BindingFlags.Public | BindingFlags.Instance)
			                         .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
			                         .ToArray();

			foreach (var field in allFields)
			{
				// tmp.<Field> = writer.ReadXYZ();
				gen.Emit(type.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc, target);
				Type fieldType = field.FieldType;

				try
				{
					EmitReadValue(gen,
					              loadReader,
					              loadSerializer,
					              loadRemotingEndPoint,
					              fieldType);
				}
				catch (SerializationException)
				{
					throw;
				}
				catch (Exception e)
				{
					var message = string.Format("There was a problem generating the code to deserialize field '{0} {1}' of type '{2}' ",
						field.FieldType,
						field.Name,
						type.Type.FullName
						);
					throw new SerializationException(message, e);
				}

				gen.Emit(OpCodes.Stfld, field);
			}
		}

		#endregion
	}
}