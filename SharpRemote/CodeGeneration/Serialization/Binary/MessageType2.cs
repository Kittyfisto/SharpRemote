using System;
using System.Runtime.Serialization;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	/// <summary>
	///     Classifies messages into method calls and -results.
	/// </summary>
	[Flags]
	[DataContract]
	internal enum MessageType2 : byte
	{
		/// <summary>
		///     The method is a call and carries target, method and parameter values.
		/// </summary>
		[EnumMember] Call = 0,

		/// <summary>
		///     The method is a result from a previous call and carries return value / exception, if available.
		/// </summary>
		[EnumMember] Result = 1,

		/// <summary>
		///     The method call resulted in an exception.
		/// </summary>
		[EnumMember] Exception = 2
	}
}