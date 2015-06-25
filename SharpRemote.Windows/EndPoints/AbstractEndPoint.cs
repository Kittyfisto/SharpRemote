using System;
using System.IO;
using System.Runtime.Serialization;


#if !SILVERLIGHT
using System.Runtime.Serialization.Formatters.Binary;
#endif

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public abstract class AbstractEndPoint
	{
		#region Static Methods

#if !SILVERLIGHT
		protected static void WriteException(BinaryWriter writer, Exception e)
		{
			Stream stream = writer.BaseStream;
			long start = stream.Position;
			var formatter = new BinaryFormatter();

			try
			{
				formatter.Serialize(stream, e);
			}
			catch (SerializationException)
			{
				// TODO: Log this..
				writer.Flush();
				stream.Position = start;
				formatter.Serialize(stream, new UnserializableException(e));
			}
		}
#endif

		#endregion
	}
}