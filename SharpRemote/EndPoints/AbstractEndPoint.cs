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
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
			catch (Exception exception)
			{
				Log.WarnFormat("Unable to serialize exception: {0}", exception);

				// TODO: Log this..
				writer.Flush();
				stream.Position = start;
				formatter.Serialize(stream, new UnserializableException(e));
			}
			// TODO: Catch SecurityException and serialize it along with the original exception (plus figure out when it'll be thrown)
		}

		internal static Exception ReadException(BinaryReader reader)
		{
			// I will never understand why the remote stacktrace is ONLY ever preserved for
			// exceptions which cross app domains. Wouldn't it also be useful when traversing machines?!
			// Anyways, this feature is super important when working with distributed software so we'll
			// have to lie here in order to get what we want...
			// (Proof: https://referencesource.microsoft.com/#mscorlib/system/exception.cs)
			var formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.CrossAppDomain));
			var e = (Exception)formatter.Deserialize(reader.BaseStream);
			// TODO: Catch both exceptions and throw an appropriate replacement informing the user of this problem
			return e;
		}
#endif
#endif

		#endregion
	}
}