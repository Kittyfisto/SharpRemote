using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	[Serializable]
	public class TypeMismatchException
		: RemotingException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		public TypeMismatchException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}
#endif
#endif

		public TypeMismatchException(string message, Exception innerException = null)
			: base(message, innerException)
		{}

		public TypeMismatchException()
		{}
	}
}