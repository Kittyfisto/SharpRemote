using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when client or server failed authentication.
	/// </summary>
	[Serializable]
	public class AuthenticationException
		: SharpRemoteException
	{
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public AuthenticationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}

		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		public AuthenticationException()
		{}

		/// <summary>
		/// Initializes a new instance of this exception with the given message and inner exception that
		/// is the cause of this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public AuthenticationException(string message, Exception innerException = null)
			: base(message, innerException)
		{
			
		}
	}
}