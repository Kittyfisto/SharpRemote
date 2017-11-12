using System;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     An exception thrown when an error occured during parsing.
	/// </summary>
	public abstract class ParseException
		: Exception
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		protected ParseException(string message, Exception innerException)
			: base(message, innerException)
		{}
	}
}