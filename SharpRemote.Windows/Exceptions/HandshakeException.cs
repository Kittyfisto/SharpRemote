using System;
using System.Runtime.Serialization;

namespace SharpRemote.Exceptions
{
	/// <summary>
	/// 
	/// </summary>
	[Serializable]
	public class HandshakeException
		: AuthenticationException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public HandshakeException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}
#endif
#endif

		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		public HandshakeException()
		{}

		/// <summary>
		/// Initializes a new instance of this exception with the given message and inner exception that
		/// is the cause of this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public HandshakeException(string message, Exception innerException = null)
			: base(message, innerException)
		{
			
		}
	}
}