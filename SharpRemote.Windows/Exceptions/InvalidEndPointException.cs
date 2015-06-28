using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	[Serializable]
	public sealed class InvalidEndPointException
		: RemotingException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		public InvalidEndPointException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			string ip = info.GetString("Address");
			Uri.TryCreate(ip, UriKind.RelativeOrAbsolute, out Uri);
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("Uri", Uri != null ? Uri.ToString() : null);
		}
#endif
#endif

		public InvalidEndPointException(Uri uri, Exception e = null)
			: base(string.Format("The given socket does not represent a valid endpoint: {0}", uri), e)
		{
			Uri = uri;
		}

		public readonly Uri Uri;
	}
}