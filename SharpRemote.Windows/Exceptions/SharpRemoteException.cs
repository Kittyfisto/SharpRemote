using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// Base exception for various exceptions (but not all) thrown by this library.
	/// </summary>
	[Serializable]
	public class SharpRemoteException
		: SystemException
	{
		/// <summary>
		/// 
		/// </summary>
		public SharpRemoteException()
		{}

#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public SharpRemoteException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}
#endif
#endif

		/// <summary>
		/// Initializes a new instance with the given message and inner exception that is the cause
		/// for this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public SharpRemoteException(string message, Exception innerException = null)
			: base(message, innerException)
		{}
	}
}