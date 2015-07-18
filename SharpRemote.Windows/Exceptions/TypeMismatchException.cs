using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a remote method call is executed between
	/// incompatible <see cref="IProxy"/> and <see cref="IServant"/> instances, for example
	/// because they implement different interfaces.
	/// </summary>
	[Serializable]
	public class TypeMismatchException
		: SharpRemoteException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public TypeMismatchException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}
#endif
#endif

		/// <summary>
		/// Initializes a new instance of this exception with the given message and inner exception
		/// that is the cause of this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public TypeMismatchException(string message, Exception innerException = null)
			: base(message, innerException)
		{}

		/// <summary>
		/// 
		/// </summary>
		public TypeMismatchException()
		{}
	}
}