using System.IO;

namespace SharpRemote
{
	public interface IGrain
	{
		ulong ObjectId { get; }
		ISerializer Serializer { get; }

		/// <summary>
		/// Shall invoke the event named <paramref cref="eventName"/>.
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		void Invoke(string eventName, BinaryReader reader, BinaryWriter writer);
	}
}