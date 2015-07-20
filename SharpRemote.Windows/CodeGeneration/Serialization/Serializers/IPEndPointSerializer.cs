using System;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	internal sealed class IPEndPointSerializer
		: AbstractTypeSerializer
	{
		private static readonly MethodInfo GetAddress;
		private static readonly MethodInfo GetPort;
		private static readonly ConstructorInfo Ctor;
		private static readonly IPAddressSerializer IPAddressSerializer;

		static IPEndPointSerializer()
		{
			GetAddress = typeof (IPEndPoint).GetProperty("Address").GetMethod;
			GetPort = typeof (IPEndPoint).GetProperty("Port").GetMethod;
			Ctor = typeof (IPEndPoint).GetConstructor(new[] {typeof (IPAddress), typeof (int)});
			IPAddressSerializer = new IPAddressSerializer();
		}

		public override bool Supports(Type type)
		{
			return type == typeof (IPEndPoint);
		}

		public override void EmitWriteValue(ILGenerator gen,
		                                    Serializer serializerCompiler,
		                                    Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress,
		                                    Action loadSerializer,
		                                    Action loadRemotingEndPoint,
		                                    Type type,
		                                    bool valueCanBeNull = true)
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
					                       IPAddressSerializer.EmitWriteValue(gen,
					                                                          serializerCompiler, loadWriter,
					                                                          () => gen.Emit(OpCodes.Ldloc, addr), loadValueAddress,
					                                                          loadSerializer,
					                                                          loadRemotingEndPoint,
					                                                          type,
					                                                          valueCanBeNull: false);

					                       loadWriter();
					                       loadValue();
					                       gen.Emit(OpCodes.Callvirt, GetPort);
					                       gen.Emit(OpCodes.Call, Methods.WriteInt32);
				                       },
			                       valueCanBeNull);
		}

		public override void EmitReadValue(ILGenerator gen,
		                                   Serializer serializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			EmitReadNullableValue(gen, loadReader, () =>
				{
					IPAddressSerializer.EmitReadValue(gen,
					                                  serializerCompiler,
					                                  loadReader,
					                                  loadSerializer,
					                                  loadRemotingEndPoint,
					                                  type,
					                                  valueCanBeNull: false);

					loadReader();
					gen.Emit(OpCodes.Call, Methods.ReadInt32);

					gen.Emit(OpCodes.Newobj, Ctor);
				},
			                      valueCanBeNull);
		}
	}
}