using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	[Serializable]
	public class NotConnectedException
		: InvalidOperationException
	{
		public readonly string EndPointName;

		public NotConnectedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			EndPointName = info.GetString("EndPointName");
		}

		public NotConnectedException(string endPointName)
			: base("This endpoint is not connected to any other endpoint")
		{
			EndPointName = endPointName;
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("EndPointName", EndPointName);
		}
	}
}