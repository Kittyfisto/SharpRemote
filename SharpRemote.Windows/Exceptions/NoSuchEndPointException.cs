using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	[Serializable]
	public sealed class NoSuchEndPointException
		: RemotingException
	{
#if !WINDOWS_PHONE_APP
		public NoSuchEndPointException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			var ip = info.GetString("Address");
			Uri.TryCreate(ip, UriKind.RelativeOrAbsolute, out Uri);
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("Uri", Uri != null ? Uri.ToString() : null);
		}
#endif

        public NoSuchEndPointException(Uri uri, Exception e = null)
			: base(string.Format("Unable to establish a connection with the given endpoint: {0}", uri), e)
		{
			Uri = uri;
		}

		public readonly Uri Uri;
	}
}