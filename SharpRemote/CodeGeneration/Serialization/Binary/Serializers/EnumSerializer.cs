using System;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class EnumSerializer
		: AbstractTypeSerializer
	{
		public override bool Supports(Type type)
		{
			return type.IsEnum;
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
			loadWriter();
			loadValue();
			gen.Emit(OpCodes.Call, Methods.WriteInt32);
		}

		public override void EmitReadValue(ILGenerator gen,
		                                   ISerializerCompiler serializerCompiler,
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