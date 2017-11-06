using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class KeyValuePairSerializer
		: AbstractTypeSerializer
	{
		#region Public Methods

		public override void EmitReadValue(ILGenerator gen,
		                                   ISerializerCompiler serializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			var keyType = type.GenericTypeArguments[0];
			var valueType = type.GenericTypeArguments[1];
			var ctor = type.GetConstructor(new[] {keyType, valueType});

			serializerCompiler.EmitReadValue(gen,
			                                 loadReader,
			                                 loadSerializer,
			                                 loadRemotingEndPoint,
			                                 keyType);

			serializerCompiler.EmitReadValue(gen,
			                                 loadReader,
			                                 loadSerializer,
			                                 loadRemotingEndPoint,
			                                 valueType);

			gen.Emit(OpCodes.Newobj, ctor);
		}

		public override void EmitWriteValue(ILGenerator gen,
		                                    ISerializerCompiler serializerCompiler,
		                                    Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress,
		                                    Action loadSerializer,
		                                    Action loadRemotingEndPoint,
		                                    Type type,
		                                    bool valueCanBeNull = true)
		{
			var keyType = type.GenericTypeArguments[0];
			var valueType = type.GenericTypeArguments[1];
			var getKey = type.GetProperty("Key").GetMethod;
			var getValue = type.GetProperty("Value").GetMethod;

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

			serializerCompiler.EmitWriteValue(gen,
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

			serializerCompiler.EmitWriteValue(gen,
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
			       type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
		}

		#endregion
	}
}