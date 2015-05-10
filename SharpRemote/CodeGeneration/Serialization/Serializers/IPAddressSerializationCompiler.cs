using System;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	public class IPAddressSerializationCompiler
		: AbstractSerializationCompiler<IPAddress>
	{
		private readonly MethodInfo _ipAddressGetAddressBytes;
		private readonly ConstructorInfo _ipAddressFromBytes;

		public IPAddressSerializationCompiler()
		{
			_ipAddressGetAddressBytes = typeof(IPAddress).GetMethod("GetAddressBytes");
			_ipAddressFromBytes = typeof(IPAddress).GetConstructor(new[] { typeof(byte[]) });
		}

		public override void EmitWriteValue(ILGenerator gen, Action loadWriter, Action loadValue, bool valueCanBeNull = true)
		{
			EmitWriteNullableValue(
				gen,
					   loadWriter,
					   loadValue,
					   () =>
					   {
						   var data = gen.DeclareLocal(typeof(byte[]));

						   loadValue();
						   gen.Emit(OpCodes.Call, _ipAddressGetAddressBytes);
						   gen.Emit(OpCodes.Stloc, data);

						   loadWriter();
						   gen.Emit(OpCodes.Ldloc, data);
						   gen.Emit(OpCodes.Call, Methods.ArrayGetLength);

						   gen.Emit(OpCodes.Call, Methods.WriteInt);

						   loadWriter();
						   gen.Emit(OpCodes.Ldloc, data);
						   gen.Emit(OpCodes.Call, Methods.WriteBytes);
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
					// new IPAddress(writer.ReadBytes(writer.ReadInt()));
					loadReader();
					loadReader();
					gen.Emit(OpCodes.Call, Methods.ReadInt);
					gen.Emit(OpCodes.Call, Methods.ReadBytes);
					gen.Emit(OpCodes.Newobj, _ipAddressFromBytes);
				},
				valueCanBeNull
				);
		}
	}
}