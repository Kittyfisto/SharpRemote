using System;
using System.IO;
using System.Threading.Tasks;
using SharpRemote.Tasks;

namespace SharpRemote
{
	/// <summary>
	/// Base interface for objects involved in remoting.
	/// </summary>
	public interface IGrain
	{
		/// <summary>
		/// The unique id of the object. A proxy and servant (over the same channel) belong
		/// together when they have the same id.
		/// </summary>
		ulong ObjectId { get; }

		/// <summary>
		/// The serialized used to serialize and deserialize method arguments, return values
		/// and exceptions.
		/// </summary>
		ISerializer Serializer { get; }

		/// <summary>
		/// The actual type of the interface that is being remoted.
		/// </summary>
		Type InterfaceType { get; }

		/// <summary>
		///     The endpoint this grain has been created by and over which all communication
		///     is performed.
		/// </summary>
		IRemotingEndPoint EndPoint { get; }

		/// <summary>
		/// Shall invoke the event or method named <paramref ref="eventOrMethodName"/>.
		/// </summary>
		/// <param name="eventOrMethodName"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		void Invoke(string eventOrMethodName, BinaryReader reader, BinaryWriter writer);

#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Returns the specific task scheduler instance that must be used to schedule an invocation
		/// of the given method or null if <see cref="TaskScheduler.Default"/> shall be used.
		/// </summary>
		/// <param name="eventOrMethodName"></param>
		/// <returns></returns>
		SerialTaskScheduler GetTaskScheduler(string eventOrMethodName);
#endif
#endif
	}
}