using System.Runtime.Serialization;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	/// <summary>
	///     Classifies messages into method calls and -results.
	/// </summary>
	[DataContract]
	internal enum MessageType2
	{
		/// <summary>
		///     The method is a call and carries target, method and parameter values.
		/// </summary>
		[EnumMember] Call = 0,

		/// <summary>
		///     The method is a result from a previous call and carries return value / exception, if available.
		/// </summary>
		[EnumMember] Result = 1
	}
}