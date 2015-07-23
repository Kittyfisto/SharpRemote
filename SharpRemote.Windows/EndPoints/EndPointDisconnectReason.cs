// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// Describes the reason why a previously connected socket is now disconnected.
	/// </summary>
	public enum EndPointDisconnectReason
	{
		/// <summary>
		/// The 
		/// </summary>
		RequestedByEndPoint,

		/// <summary>
		/// The remote endpoint that this one is connected to requested the connection to be dropped.
		/// </summary>
		RequestedByRemotEndPoint,

		/// <summary>
		/// The connection was dropped because a read operation on the socket failed.
		/// </summary>
		ReadFailure,

		/// <summary>
		/// The connection was dropped because a write operation on the socket failed.
		/// </summary>
		WriteFailure,

		/// <summary>
		/// The connection was dropped because a response to a non-existant pending RPC was received.
		/// </summary>
		RpcInvalidResponse,

		/// <summary>
		/// The connection was dropped because an unexpected 
		/// </summary>
		UnhandledException,
	}
}