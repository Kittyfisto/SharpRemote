using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Exceptions
{
	[Serializable]
	public class WellBehavedCustomException
		: NotImplementedException
	{
		public WellBehavedCustomException(string message)
			: base(message)
		{

		}

		public WellBehavedCustomException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
	}
}
