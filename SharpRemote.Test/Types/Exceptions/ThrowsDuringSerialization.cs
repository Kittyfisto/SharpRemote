using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Exceptions
{
	[Serializable]
	public sealed class ThrowsDuringSerialization
		: Exception
	{
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			throw new NullReferenceException();
		}
	}
}