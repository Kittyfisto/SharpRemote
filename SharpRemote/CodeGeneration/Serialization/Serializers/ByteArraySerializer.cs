using System;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	public sealed class ByteArraySerializer
		: AbstractTypeSerializer<byte[]>
	{
		public override void EmitWriteValue(ILGenerator gen, Action loadWriter, Action loadValue, bool valueCanBeNull = true)
		{
			EmitWriteNullableValue(gen, loadWriter, loadValue, () =>
				{
					loadWriter();
					loadValue();
					gen.Emit(OpCodes.Ldlen);
					gen.Emit(OpCodes.Conv_I4);
					gen.Emit(OpCodes.Call, Methods.WriteInt);

					loadWriter();
					loadValue();
					gen.Emit(OpCodes.Call, Methods.WriteBytes);
				},
			                       valueCanBeNull);
		}

		public override void EmitReadValue(ILGenerator gen, Action loadReader, bool valueCanBeNull = true)
		{
			EmitReadNullableValue(gen, loadReader, () =>
				{
					loadReader();
					loadReader();
					gen.Emit(OpCodes.Call, Methods.ReadInt);
					gen.Emit(OpCodes.Call, Methods.ReadBytes);
				},
				valueCanBeNull);
		}
	}
}