using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when client or server requires authentication, but the other side
	/// doesn't provide any.
	/// </summary>
	[Serializable]
	public class AuthenticationRequiredException
		: AuthenticationException
	{
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public AuthenticationRequiredException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }

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