using System;

namespace SharpRemote.Extensions
{
	/// <summary>
	///     Extensions to the byte array.
	/// </summary>
	public static class ByteArrayExtensions
	{
		/// <summary>
		///     Prints the contents of this array as a c# statement in the form of
		///     "var data = new byte[]{ content }".
		/// </summary>
		/// <param name="that"></param>
		public static void Print(this byte[] that)
		{
			var hex = BitConverter.ToString(that).Replace("-", ", 0x");
			Console.WriteLine("var data = new byte[]{0};", "{0x" + hex + "}");
		}
	}
}