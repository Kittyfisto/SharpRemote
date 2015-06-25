using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	[Serializable]
	public class RemotingException
		: SystemException
	{
		public RemotingException()
		{}

#if !WINDOWS_PHONE_APP
		public RemotingException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}
#endif

		public RemotingException(string message, Exception e = null)
			: base(message, e)
		{}
	}
}