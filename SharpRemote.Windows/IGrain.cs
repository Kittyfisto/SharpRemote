using System.IO;

namespace SharpRemote
{
	public interface IGrain
	{
		ulong ObjectId { get; }
		ISerializer Serializer { get; }

		/// <summary>
		/// Shall invoke the event or method named <paramref cref="eventOrMethodName"/>.
		/// </summary>
		/// <param name="eventOrMethodName"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		void Invoke(string eventOrMethodName, BinaryReader reader, BinaryWriter writer);
	}
}