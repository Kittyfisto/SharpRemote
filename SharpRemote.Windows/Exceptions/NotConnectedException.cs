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

#if !WINDOWS_PHONE_APP
		public NotConnectedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			EndPointName = info.GetString("EndPointName");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("EndPointName", EndPointName);
		}
#endif

        public NotConnectedException(string endPointName)
			: base("This endpoint is not connected to any other endpoint")
		{
			EndPointName = endPointName;
		}

	}
}