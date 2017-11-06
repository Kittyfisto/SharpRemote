namespace SharpRemote
{
	/// <summary>
	///     Describes to a <see cref="AbstractBinaryStreamEndPoint{T}" /> if the current endpoint
	///     is supposed to be a server or a client.
	/// </summary>
	public enum EndPointType
	{
		/// <summary>
		///     The endpoint is a client:
		///     Methods such as <see cref="ISocketEndPoint.Connect(string,System.TimeSpan)" />
		///     and <see cref="ISocketEndPoint.TryConnect(string)" /> may be used.
		/// </summary>
		Client,

		/// <summary>
		///     The endpoint is a server:
		///     Methods such as <see cref="ISocketEndPoint.Bind(System.Net.IPEndPoint)" /> or
		///     <see cref="ISocketEndPoint.Bind(System.Net.IPAddress)" /> may be used.
		/// </summary>
		Server
	}
}