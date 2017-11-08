// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     This enum is used while serializing a type that is attributed with the <see cref="ByReferenceAttribute" />.
	///     It instructs the deserializer on what exactly it should do.
	/// </summary>
	internal enum ByReferenceHint : byte
	{
		/// <summary>
		///     The deserializer should create a proxy to represent the subject of the other endpoint.
		/// </summary>
		CreateProxy = 0,

		/// <summary>
		///     The deserializer should retrieve the original subject (this happens when a proxy is sent back
		///     to the endpoint which registered the subject in the first place).
		/// </summary>
		RetrieveSubject = 1
	}
}