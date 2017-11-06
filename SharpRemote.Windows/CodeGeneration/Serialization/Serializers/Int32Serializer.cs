using System;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	internal sealed class Int32Serializer
		: AbstractTypeSerializer
	{
		public override bool Supports(Type type)
		{
			return type == typeof (Int32);
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
			loadWriter();
			loadValue();
			gen.Emit(OpCodes.Call, Methods.WriteInt32);
		}

		public override void EmitReadValue(ILGenerator gen,
		                                   BinarySerializer binarySerializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);
		}
	}
}