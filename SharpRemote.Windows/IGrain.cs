using System;
using System.IO;
using System.Threading.Tasks;

namespace SharpRemote
{
	public interface IGrain
	{
		ulong ObjectId { get; }
		ISerializer Serializer { get; }
		Type InterfaceType { get; }

		/// <summary>
		/// Shall invoke the event or method named <paramref cref="eventOrMethodName"/>.
		/// </summary>
		/// <param name="eventOrMethodName"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		void Invoke(string eventOrMethodName, BinaryReader reader, BinaryWriter writer);

		TaskScheduler GetTaskScheduler(string eventOrMethodName);
	}
}