using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This exception is thrown when a synchronous method on the proxy, or an event on the servant, is called and
	///     the call did not **fully** succeed.
	/// </summary>
	[Serializable]
	public class RemoteProcedureCallCanceledException
		: OperationCanceledException
	{
		/// <summary>
		///     Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public RemoteProcedureCallCanceledException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		///     Initializes a new instance of this exception.
		/// </summary>
		public RemoteProcedureCallCanceledException()
			: base("The remote procedure call has been canceled")
		{
		}

		/// <summary>
		///     Initializes a new instance of this exception.
		/// </summary>
		/// <param name="message"></param>
		public RemoteProcedureCallCanceledException(string message)
			: base(message)
		{
		}

		/// <summary>
		///     Initializes a new instance of this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public RemoteProcedureCallCanceledException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}