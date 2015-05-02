using System.IO;

namespace SharpRemote
{
	public interface IProxy
		: IGrain
	{
		/// <summary>
		/// Shall invoke the event named <paramref cref="eventName"/>.
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		void InvokeEvent(string eventName, BinaryReader reader, BinaryWriter writer);
	}
}