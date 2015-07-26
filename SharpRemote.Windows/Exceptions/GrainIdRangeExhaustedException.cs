using System;
using System.Runtime.Serialization;

namespace SharpRemote.Exceptions
{
	/// <summary>
	/// This exception is thrown when no more <see cref="GrainId"/>s can be generated
	/// because the key range is exhausted.
	/// </summary>
	[Serializable]
	public class GrainIdRangeExhaustedException
		: SharpRemoteException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public GrainIdRangeExhaustedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}
#endif
#endif

		/// <summary>
		/// 
		/// </summary>
		public GrainIdRangeExhaustedException()
			: base("The range of available grain ids has been exhausted - no more can be generated")
		{}
	}
}