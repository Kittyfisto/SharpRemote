using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using SharpRemote.Extensions;

namespace SharpRemote.Test.Remoting.Sockets
{
	public sealed class TcpPortBlocker
		: IDisposable
	{
		private readonly List<Socket> _sockets;

		public TcpPortBlocker(ushort minPort, ushort maxBlockedPort)
		{
			_sockets = new List<Socket>(maxBlockedPort - minPort);
			for (ushort port = minPort; port < maxBlockedPort; ++port)
			{
				var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
				_sockets.Add(socket);
			}
		}

		#region IDisposable

		public void Dispose()
		{
			foreach (var socket in _sockets)
			{
				socket.TryDispose();
			}
		}

		#endregion
	}
}