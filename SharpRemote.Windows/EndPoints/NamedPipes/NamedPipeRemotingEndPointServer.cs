using System;
using System.IO.Pipes;
using SharpRemote.Exceptions;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class NamedPipeRemotingEndPointServer
		: AbstractNamedPipeEndPoint<NamedPipeServerStream>
	{
		private NamedPipeServerStream _pipe;
		private bool _isConnecting;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="clientAuthenticator"></param>
		/// <param name="serverAuthenticator"></param>
		/// <param name="customTypeResolver"></param>
		/// <param name="serializer"></param>
		/// <param name="heartbeatSettings"></param>
		/// <param name="latencySettings"></param>
		/// <param name="endPointSettings"></param>
		public NamedPipeRemotingEndPointServer(string name = null,
											   IAuthenticator clientAuthenticator = null,
											   IAuthenticator serverAuthenticator = null,
											   ITypeResolver customTypeResolver = null,
											   Serializer serializer = null,
											   HeartbeatSettings heartbeatSettings = null,
											   LatencySettings latencySettings = null,
											   EndPointSettings endPointSettings = null)
			: base(name, EndPointType.Server,
			       clientAuthenticator,
			       serverAuthenticator,
			       customTypeResolver,
			       serializer,
			       heartbeatSettings,
			       latencySettings,
			       endPointSettings)
		{
		}

		/// <summary>
		/// Binds this endpoint to <see cref="IRemotingEndPoint.Name"/>.
		/// </summary>
		public void Bind()
		{
			Bind(new NamedPipeEndPoint(Name, NamedPipeEndPoint.PipeType.Server));
		}

		/// <summary>
		/// Binds this endpoint to the given name.
		/// Once bound, incoming connections may be accepted.
		/// </summary>
		/// <param name="endPoint"></param>
		public void Bind(NamedPipeEndPoint endPoint)
		{
			if (endPoint == null) throw new ArgumentNullException("endPoint");
			if (LocalEndPoint != null)
				throw new InvalidOperationException("This endpoint is already bound");

			LocalEndPoint = endPoint;
			_pipe = new NamedPipeServerStream(endPoint.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			_pipe.BeginWaitForConnection(OnIncomingConnection, null);
		}

		private void OnIncomingConnection(IAsyncResult ar)
		{
			lock (SyncRoot)
			{
				if (IsDisposed)
					return;
			}

			NamedPipeServerStream socket = null;
			bool success = false;
			try
			{
				_pipe.EndWaitForConnection(ar);
				socket = _pipe;

				bool isAlreadyConnected;
				lock (SyncRoot)
				{
					isAlreadyConnected = InternalRemoteEndPoint != null ||
										 _isConnecting;
					_isConnecting = true;
				}

				if (isAlreadyConnected)
				{
					Log.InfoFormat("Blocking incoming connection from '', we're already connected to another endpoint");
				}
				else
				{
					Log.DebugFormat("Incoming connection from '', starting handshake...");

					var remoteEndPoint = NamedPipeEndPoint.FromClient(Name);
					var connectionId = PerformIncomingHandshake(_pipe, remoteEndPoint);
					FireOnConnected(remoteEndPoint, connectionId);

					success = true;
				}
			}
			catch (AuthenticationException e)
			{
				Log.WarnFormat("Closing connection: {0}", e);

				Disconnect();
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught exception while accepting incoming connection - disconnecting again: {0}", e);

				Disconnect();
			}
			finally
			{
				if (!success)
				{
					if (socket != null)
					{
						try
						{
							socket.Disconnect();
						}
						catch (Exception e)
						{
							Log.WarnFormat("Ignoring exception caught while disconnecting & disposing of socket: {0}", e);
						}
					}

					lock (SyncRoot)
					{
						if (!IsDisposed)
						{
							_pipe.BeginWaitForConnection(OnIncomingConnection, null);
						}
					}
				}
				_isConnecting = false;
			}
		}

		protected override void DisposeAdditional()
		{
			var pipe = _pipe;
			if (pipe != null)
			{
				pipe.Dispose();
				_pipe = null;
			}
		}

		protected override void DisconnectTransport(NamedPipeServerStream socket, bool reuseSocket)
		{
			socket.Disconnect();
		}

		protected override void DisposeAfterDisconnect(NamedPipeServerStream socket)
		{
			// We don't do anything because we want to re-use this exact pipe for the next connection.
			// Contrary to Sockets, we don't have a separate object for every incoming connection.
			// Hence we don't have anything to dispose of.

			// We do however want to start accepting new incoming connections...
			lock (SyncRoot)
			{
				if (!IsDisposed)
				{
					_pipe.BeginWaitForConnection(OnIncomingConnection, null);
				}
			}
		}
	}
}