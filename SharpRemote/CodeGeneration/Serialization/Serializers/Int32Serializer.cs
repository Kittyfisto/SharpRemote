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

		public override void EmitWriteValue(ILGenerator gen, Serializer serializerCompiler, Action loadWriter, Action loadValue, Action loadValueAddress, Action loadSerializer, Type type, bool valueCanBeNull = true)
		{
			loadWriter();
			loadValue();
			gen.Emit(OpCodes.Call, Methods.WriteInt32);
		}

		public override void EmitReadValue(ILGenerator gen, Serializer serializerCompiler, Action loadReader, Action loadSerializer, Type type, bool valueCanBeNull = true)
		{
			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);
		}
	}
}