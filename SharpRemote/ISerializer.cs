using System;
using System.IO;

namespace SharpRemote
{
	public interface ISerializer
	{
		void WriteObject(BinaryWriter writer, object value);
		object Deserialize(BinaryReader reader, Type type);
	}
}