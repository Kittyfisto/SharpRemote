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
	public class NoSuchServantException
		: SharpRemoteException
	{
		/// <summary>
		/// The <see cref="IGrain.ObjectId"/> that could not be found on the callee's side.
		/// </summary>
		public readonly ulong ObjectId;

#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public NoSuchServantException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			ObjectId = info.GetUInt64("ObjectId");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("ObjectId", ObjectId);
		}
#endif
#endif

		/// <summary>
		/// Initializes a new instance of this exception with the given object id.
		/// </summary>
		/// <param name="objectId"></param>
		public NoSuchServantException(ulong objectId)
			: base(string.Format("No such servant: {0}", objectId))
		{}
	}
}