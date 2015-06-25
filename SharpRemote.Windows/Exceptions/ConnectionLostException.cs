using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a synchronous method on the proxy, or an event on the servant, is called and the connection
	/// has been interrupted while the call was ongoing.
	/// </summary>
	[Serializable]
	public class ConnectionLostException
		: OperationCanceledException
	{
#if !WINDOWS_PHONE_APP
		public ConnectionLostException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}
#endif

		public ConnectionLostException()
			: base("The connection to the remote endpoint has been lost")
		{}
	}
}