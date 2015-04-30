using System;

namespace SharpRemote.Test.CodeGeneration.Types.Exceptions
{
	[Serializable]
	public sealed class ThrowsDuringSerialization
		: Exception
	{
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			base.GetObjectData(info, context);

			throw new NullReferenceException();
		}
	}
}