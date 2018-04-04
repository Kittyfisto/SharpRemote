using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a type or message is encountered that cannot be serialized / deserialized.
	/// </summary>
	[Serializable]
	public class SerializationException
		: SystemException
	{
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public SerializationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}

		/// <summary>
		/// Initializes a new instance of this exception with the given message and inner exception
		/// that is the cause of this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public SerializationException(string message, Exception innerException = null)
			: base(message, innerException)
		{}

		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		public SerializationException()
		{}
	}
}
