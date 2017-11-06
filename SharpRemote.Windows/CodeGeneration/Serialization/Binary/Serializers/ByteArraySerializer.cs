using System;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class ByteArraySerializer
		: AbstractTypeSerializer
	{
		public override bool Supports(Type type)
		{
			return type == typeof(byte[]);
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
			EmitWriteNullableValue(gen, loadWriter, loadValue, () =>
			                       {
				                       loadWriter();
				                       loadValue();
				                       gen.Emit(OpCodes.Ldlen);
				                       gen.Emit(OpCodes.Conv_I4);
				                       gen.Emit(OpCodes.Call, Methods.WriteInt32);

				                       loadWriter();
				                       loadValue();
				                       gen.Emit(OpCodes.Call, Methods.WriteBytes);
			                       },
			                       valueCanBeNull);
		}

		public override void EmitReadValue(ILGenerator gen,
		                                   ISerializerCompiler serializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			EmitReadNullableValue(gen, loadReader, () =>
			                      {
				                      loadReader();
				                      loadReader();
				                      gen.Emit(OpCodes.Call, Methods.ReadInt32);
				                      gen.Emit(OpCodes.Call, Methods.ReadBytes);
			                      },
			                      valueCanBeNull);
		}
	}
}