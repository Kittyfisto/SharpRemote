using System.Net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	public interface ISocketServer
		: IRemotingServer
	{
		/// <summary>
		///     The endpoint this socket is bound to.
		/// </summary>
		new IPEndPoint LocalEndPoint { get; }

		#region Bind

		/// <summary>
		///     Binds this socket to the given endpoint.
		/// </summary>
		/// <param name="ep"></param>
		void Bind(IPEndPoint ep);

		/// <summary>
		///     Binds this socket to the given address.
		/// </summary>
		/// <param name="localAddress"></param>
		void Bind(IPAddress localAddress);

		#endregion
	}
}