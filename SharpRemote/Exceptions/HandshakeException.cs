using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	[Serializable]
	public class HandshakeException
		: AuthenticationException
	{
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public HandshakeException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}

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