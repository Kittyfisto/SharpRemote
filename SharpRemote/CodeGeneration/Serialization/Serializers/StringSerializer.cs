using System;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	public sealed class StringSerializer
		: AbstractTypeSerializer<string>
	{
		public override void EmitWriteValue(ILGenerator gen, Action loadWriter, Action loadValue, bool valueCanBeNull = true)
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

		public override void EmitReadValue(ILGenerator gen, Action loadReader, bool valueCanBeNull = true)
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