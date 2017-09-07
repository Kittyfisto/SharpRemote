using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     Base class for several endpoint implementations.
	/// </summary>
	public abstract class AbstractEndPoint
	{
		/// <summary>
		/// log4net logger...
		/// </summary>
		protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		#region Static Methods

#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Writes the given exception using the given writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="e"></param>
		internal static void WriteException(BinaryWriter writer, Exception e)
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
			// TODO: Catch SecurityException and serialize it along with the original exception (plus figure out when it'll be thrown)
		}

		internal static Exception ReadException(BinaryReader reader)
		{
			var formatter = new BinaryFormatter();
			var e = (Exception)formatter.Deserialize(reader.BaseStream);
			// TODO: Catch both exceptions and throw an appropriate replacement informing the user of this problem
			return e;
		}
#endif
#endif

		#endregion
	}
}