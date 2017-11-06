using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class KeyValuePairSerializer
		: AbstractTypeSerializer
	{
		#region Public Methods

		public override void EmitReadValue(ILGenerator gen,
			BinarySerializer binarySerializerCompiler,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			Type type,
			bool valueCanBeNull = true)
		{
			Type keyType = type.GenericTypeArguments[0];
			Type valueType = type.GenericTypeArguments[1];
			ConstructorInfo ctor = type.GetConstructor(new[] {keyType, valueType});

			binarySerializerCompiler.EmitReadValue(gen,
				loadReader,
				loadSerializer,
				loadRemotingEndPoint,
				keyType);

			binarySerializerCompiler.EmitReadValue(gen,
				loadReader,
				loadSerializer,
				loadRemotingEndPoint,
				valueType);

			gen.Emit(OpCodes.Newobj, ctor);
		}

		public override void EmitWriteValue(ILGenerator gen,
		                                    BinarySerializer binarySerializerCompiler,
		                                    Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress,
		                                    Action loadSerializer,
		                                    Action loadRemotingEndPoint,
		                                    Type type,
		                                    bool valueCanBeNull = true)
		{
			Type keyType = type.GenericTypeArguments[0];
			Type valueType = type.GenericTypeArguments[1];
			MethodInfo getKey = type.GetProperty("Key").GetMethod;
			MethodInfo getValue = type.GetProperty("Value").GetMethod;

			Action loadKeyValue = () =>
				{
					loadValueAddress();
					gen.Emit(OpCodes.Call, getKey);
				};
			LocalBuilder key = null;
			Action loadKeyValueAddress = () =>
				{
					if (key == null)
					{
						key = gen.DeclareLocal(keyType);
						loadValue();
						gen.Emit(OpCodes.Stloc, key);
					}

					gen.Emit(OpCodes.Ldloca, key);
				};

			binarySerializerCompiler.EmitWriteValue(gen,
			                                  loadWriter,
			                                  loadKeyValue,
			                                  loadKeyValueAddress,
			                                  loadSerializer,
			                                  loadRemotingEndPoint,
			                                  keyType
				);

			Action loadValueValue = () =>
				{
					loadValueAddress();
					gen.Emit(OpCodes.Call, getValue);
				};
			LocalBuilder value = null;
			Action loadValueValueAddress = () =>
				{
					if (value == null)
					{
						value = gen.DeclareLocal(valueType);
						loadValueValue();
						gen.Emit(OpCodes.Stloc, value);
					}
					gen.Emit(OpCodes.Ldloca, value);
				};

			binarySerializerCompiler.EmitWriteValue(gen,
			                                  loadWriter,
			                                  loadValueValue,
			                                  loadValueValueAddress,
			                                  loadSerializer,
			                                  loadRemotingEndPoint,
			                                  valueType);
		}

		public override bool Supports(Type type)
		{
			return type.IsGenericType &&
				type.GetGenericTypeDefinition() == typeof (KeyValuePair<,>);
		}

		#endregion
	}
}