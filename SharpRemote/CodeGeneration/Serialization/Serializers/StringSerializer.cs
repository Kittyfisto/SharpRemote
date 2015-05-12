using System;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	internal sealed class StringSerializer
		: AbstractTypeSerializer
	{
		public override bool Supports(Type type)
		{
			return type == typeof (string);
		}

		public override void EmitWriteValue(ILGenerator gen, Serializer serializerCompiler, Action loadWriter, Action loadValue, Action loadValueAddress, Action loadSerializer, Type type, bool valueCanBeNull = true)
		{
			EmitWriteNullableValue(
				gen,
				loadWriter,
				loadValue,
				() =>
				{
					loadWriter();
					loadValue();
					gen.Emit(OpCodes.Call, Methods.WriteString);
				},
				valueCanBeNull);
		}

		public override void EmitReadValue(ILGenerator gen, Serializer serializerCompiler, Action loadReader, Action loadSerializer, Type type, bool valueCanBeNull = true)
		{
			EmitReadNullableValue(
				gen,
				loadReader,
				() =>
				{
					loadReader();
					gen.Emit(OpCodes.Call, Methods.ReadString);
				},
				valueCanBeNull);
		}
	}
}