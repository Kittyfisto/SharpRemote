namespace SharpRemote
{
	/// <summary>
	///     Describes to a <see cref="AbstractBinaryStreamEndPoint{T}" /> if the current endpoint
	///     is supposed to be a server or a client.
	/// </summary>
	public enum EndPointType
	{
		/// <summary>
		///     The endpoint is a client.
		/// </summary>
		Client,

		/// <summary>
		///     The endpoint is a server.
		/// </summary>
		Server
	}
}