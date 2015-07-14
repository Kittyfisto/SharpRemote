using System;
using System.Runtime.Serialization;

namespace SharpRemote.Exceptions
{
	/// <summary>
	/// This exception is thrown when client or server requires authentication, but the other side
	/// doesn't provide any.
	/// </summary>
	public class AuthenticationRequiredException
		: AuthenticationException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public AuthenticationRequiredException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
#endif
#endif

		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		public AuthenticationRequiredException()
		{ }

		/// <summary>
		/// Initializes a new instance of this exception with the given message and inner exception that
		/// is the cause of this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public AuthenticationRequiredException(string message, Exception innerException = null)
			: base(message, innerException)
		{

		}
	}
}