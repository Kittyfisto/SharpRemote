using System;
using System.Runtime.Serialization;

namespace SharpRemote.Exceptions
{
	public sealed class NoSuchApplicationException
		: ArgumentException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		public NoSuchApplicationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			ApplicationName = info.GetString("ApplicationName");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("ApplicationName", ApplicationName);
		}
#endif
#endif

		public NoSuchApplicationException(string applicationName, Exception e = null)
			: base(string.Format("There is no installed application with this name: {0}", applicationName), e)
		{
			ApplicationName = applicationName;
		}

		public readonly string ApplicationName;
	}
}