// ReSharper disable CheckNamespace

using System.Net;

namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     The interface for an endpoint that can be connected to another <see cref="ISocketRemotingEndPoint" />.
	/// </summary>
	public interface ISocketRemotingEndPoint
		: IRemotingEndPoint
	{
		/// <summary>
		///     IPAddress+Port pair of the connected endPoint in case <see cref="SocketRemotingEndPointClient.Connect(IPEndPoint)" /> has been called.
		///     Otherwise null.
		/// </summary>
		new IPEndPoint RemoteEndPoint { get; }

		/// <summary>
		///     IPAddress+Port pair of this endPoint in case <see cref="SocketRemotingEndPointServer.Bind(IPAddress)" />
		///     or
		///     has been called.
		///     Otherwise null.
		/// </summary>
		new IPEndPoint LocalEndPoint { get; }
	}
}