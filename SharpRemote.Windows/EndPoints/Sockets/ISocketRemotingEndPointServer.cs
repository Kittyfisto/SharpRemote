// ReSharper disable CheckNamespace

using System.Net;

namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     The interface for an endpoint that can be bound to a <see cref="EndPoint" />.
	///     Accepts connections from a <see cref="ISocketRemotingEndPointClient" />.
	/// </summary>
	public interface ISocketRemotingEndPointServer
		: ISocketRemotingEndPoint
	{
	}
}