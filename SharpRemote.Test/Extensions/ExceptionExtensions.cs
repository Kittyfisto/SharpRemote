using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SharpRemote.Test.Extensions
{
	public static class ExceptionExtensions
	{
		/// <summary>
		///     Performs a serialization roundtrip using <see cref="BinaryFormatter" />.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="that"></param>
		/// <returns></returns>
		public static T Roundtrip<T>(this T that)
		{
			using (var stream = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(stream, that);

				stream.Position = 0;
				var exception = formatter.Deserialize(stream);
				return (T) exception;
			}
		}
	}
}