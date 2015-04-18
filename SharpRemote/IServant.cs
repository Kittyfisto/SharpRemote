using System.IO;

namespace SharpRemote
{
	public interface IServant
		: IGrain
	{
		object Subject { get; }

		void Invoke(string methodName, BinaryReader reader, BinaryWriter writer);
	}
}