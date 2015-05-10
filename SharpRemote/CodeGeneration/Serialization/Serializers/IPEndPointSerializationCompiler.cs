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

		static IPEndPointSerializationCompiler()
		{
			GetAddress = typeof (IPEndPoint).GetProperty("Address").GetMethod;
			GetPort = typeof (IPEndPoint).GetProperty("Port").GetMethod;
		}

		public override void EmitWriteValue(ILGenerator gen, Action loadWriter, Action loadValue, bool valueCanBeNull = true)
		{
			EmitWriteNullableValue(gen,
			                       loadWriter,
			                       loadValue,
			                       () =>
				                       {

				                       },
			                       valueCanBeNull);
		}

		public override void EmitReadValue(ILGenerator gen, Action loadReader, bool valueCanBeNull = true)
		{
			EmitReadNullableValue(gen, loadReader, () =>
				{

				},
			                      valueCanBeNull);
		}
	}
}