using System;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	public sealed class IPEndPointSerializationCompiler
		: AbstractSerializationCompiler<IPEndPoint>
	{
		private static readonly MethodInfo GetAddress;
		private static readonly MethodInfo GetPort;
		private static readonly ConstructorInfo Ctor;
		private static readonly IPAddressSerializationCompiler IPAddressSerializer;

		static IPEndPointSerializationCompiler()
		{
			GetAddress = typeof (IPEndPoint).GetProperty("Address").GetMethod;
			GetPort = typeof (IPEndPoint).GetProperty("Port").GetMethod;
			Ctor = typeof (IPEndPoint).GetConstructor(new[] {typeof (IPAddress), typeof (int)});
			IPAddressSerializer = new IPAddressSerializationCompiler();
		}

		public override void EmitWriteValue(ILGenerator gen, Action loadWriter, Action loadValue, bool valueCanBeNull = true)
		{
			EmitWriteNullableValue(gen,
			                       loadWriter,
			                       loadValue,
			                       () =>
				                       {
					                       var addr = gen.DeclareLocal(typeof (IPAddress));
					                       loadValue();
										   gen.Emit(OpCodes.Callvirt, GetAddress);
										   gen.Emit(OpCodes.Stloc, addr);
					                       IPAddressSerializer.EmitWriteValue(gen, loadWriter,
					                                                          () => gen.Emit(OpCodes.Ldloc, addr),
																			  false);

					                       loadWriter();
					                       loadValue();
										   gen.Emit(OpCodes.Callvirt, GetPort);
										   gen.Emit(OpCodes.Call, Methods.WriteInt);
				                       },
			                       valueCanBeNull);
		}

		public override void EmitReadValue(ILGenerator gen, Action loadReader, bool valueCanBeNull = true)
		{
			EmitReadNullableValue(gen, loadReader, () =>
				{
					IPAddressSerializer.EmitReadValue(gen,
					                                  loadReader,
													  false);

					loadReader();
					gen.Emit(OpCodes.Call, Methods.ReadInt);

					gen.Emit(OpCodes.Newobj, Ctor);
				},
			                      valueCanBeNull);
		}
	}
}