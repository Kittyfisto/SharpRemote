using System;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class IPAddressSerializer
		: AbstractTypeSerializer
	{
		private readonly MethodInfo _ipAddressGetAddressBytes;
		private readonly ConstructorInfo _ipAddressFromBytes;

		public IPAddressSerializer()
		{
			_ipAddressGetAddressBytes = typeof(IPAddress).GetMethod("GetAddressBytes");
			_ipAddressFromBytes = typeof(IPAddress).GetConstructor(new[] { typeof(byte[]) });
		}

		public override bool Supports(Type type)
		{
			return type == typeof (IPAddress);
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
			EmitWriteNullableValue(
				gen,
				loadWriter,
				loadValue,
				() =>
					{
						var data = gen.DeclareLocal(typeof (byte[]));

						loadValue();
						gen.Emit(OpCodes.Call, _ipAddressGetAddressBytes);
						gen.Emit(OpCodes.Stloc, data);

						loadWriter();
						gen.Emit(OpCodes.Ldloc, data);
						gen.Emit(OpCodes.Call, Methods.ArrayGetLength);

						gen.Emit(OpCodes.Call, Methods.WriteInt32);

						loadWriter();
						gen.Emit(OpCodes.Ldloc, data);
						gen.Emit(OpCodes.Call, Methods.WriteBytes);
					},
				valueCanBeNull);
		}

		public override void EmitReadValue(ILGenerator gen,
		                                   BinarySerializer binarySerializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			EmitReadNullableValue(
				gen,
				loadReader,
				() =>
					{
						// new IPAddress(writer.ReadBytes(writer.ReadInt()));
						loadReader();
						loadReader();
						gen.Emit(OpCodes.Call, Methods.ReadInt32);
						gen.Emit(OpCodes.Call, Methods.ReadBytes);
						gen.Emit(OpCodes.Newobj, _ipAddressFromBytes);
					},
				valueCanBeNull
				);
		}
	}
}