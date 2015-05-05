using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a synchronous call on a proxy, or an event on a subject is invoked, but the other endpoint currently
	/// does not have a servant with the given id registered.
	/// </summary>
	[Serializable]
	public class NoSuchServantException : RemotingException
	{
		public ulong ObjectId;

		public NoSuchServantException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			ObjectId = info.GetUInt64("ObjectId");
		}

		public NoSuchServantException(ulong objectId)
			: base(string.Format("No such servant: {0}", objectId))
		{}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("ObjectId", ObjectId);
		}
	}
}