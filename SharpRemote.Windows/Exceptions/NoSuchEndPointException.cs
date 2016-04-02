using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a connection to a non-existing / unreachable endpoint is established.
	/// </summary>
	[Serializable]
	public class NoSuchEndPointException
		: SharpRemoteException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public NoSuchEndPointException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}
#endif
#endif

		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public NoSuchEndPointException(string message, Exception innerException = null)
			: base(message, innerException)
		{}

		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		public NoSuchEndPointException()
		{}
	}
}