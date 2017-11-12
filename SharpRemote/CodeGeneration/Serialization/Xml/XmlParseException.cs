using System;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     An exception thrown when an error occured during XML parsing.
	/// </summary>
	public sealed class XmlParseException
		: ParseException
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="lineNumber"></param>
		/// <param name="linePosition"></param>
		/// <param name="innerException"></param>
		public XmlParseException(string message, int lineNumber, int linePosition, Exception innerException = null)
			: base(string.Format("Line {0}, Char {1}: {2}", lineNumber, linePosition, message), innerException)
		{
			
		}
	}
}