using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     <see cref="IRemotingEndPoint" /> implementation that establishes a TCP socket with another
	///     endPoint. A listening socket is opened (and bound to an address) with <see cref="SocketRemotingEndPointServer.Bind(IPAddress)" /> while
	///     a connectiong to such a socket is established with <see cref="SocketRemotingEndPointClient.Connect(IPEndPoint)" /> or
	///     <see cref="SocketRemotingEndPointClient.Connect(string)" />.
	/// </summary>
	public abstract class AbstractIPSocketRemotingEndPoint
		: AbstractSocketRemotingEndPoint
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private Thread _readThread;
		private Thread _writeThread;
		private IPEndPoint _remoteEndPoint;
		private IPEndPoint _localEndPoint;

		internal AbstractIPSocketRemotingEndPoint(EndPointType type,
		                                          string name = null,
		                                          IAuthenticator clientAuthenticator = null,
		                                          IAuthenticator serverAuthenticator = null,
		                                          ITypeResolver customTypeResolver = null,
		                                          Serializer serializer = null,
		                                          HeartbeatSettings heartbeatSettings = null,
		                                          LatencySettings latencySettings = null,
		                                          EndPointSettings endPointSettings = null)
			: base(new GrainIdGenerator(type),
			       name,
			       type,
			       clientAuthenticator,
			       serverAuthenticator,
			       customTypeResolver,
			       serializer,
			       heartbeatSettings,
			       latencySettings,
			       endPointSettings)
		{}

		/// <summary>
		///     IPAddress+Port pair of the connected endPoint in case <see cref="SocketRemotingEndPointClient.Connect(IPEndPoint)" /> has been called.
		///     Otherwise null.
		/// </summary>
		public new IPEndPoint RemoteEndPoint
		{
			get { return _remoteEndPoint; }
			protected set { _remoteEndPoint = value; }
		}

		protected override EndPoint InternalRemoteEndPoint
		{
			get { return _remoteEndPoint; }
			set { _remoteEndPoint = (IPEndPoint)value; }
		}

		/// <summary>
		///     IPAddress+Port pair of this endPoint in case <see cref="SocketRemotingEndPointServer.Bind(IPAddress)" /> 
		/// or 
		/// has been called.
		///     Otherwise null.
		/// </summary>
		public new IPEndPoint LocalEndPoint
		{
			get { return _localEndPoint; }
			protected set { _localEndPoint = value; }
		}

		protected override EndPoint InternalLocalEndPoint
		{
			get { return _localEndPoint; }
		}

		/// <summary>
		///     Whether or not the P2P name publishing service is available on this machine or not.
		///     Is required to <see cref="SocketRemotingEndPointServer.Bind(IPAddress)" /> a socket to a particular name (as well as a particular port)
		///     and to <see cref="SocketRemotingEndPointClient.Connect(string)" /> to that socket.
		/// </summary>
		public static bool IsP2PAvailable
		{
			get
			{
				ServiceController sc = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == "PNRPsvc");

				if (sc == null)
					return false;

				if (sc.Status == ServiceControllerStatus.Running)
					return true;

				return false;
			}
		}

		protected override void OnHandshakeSucceeded(Socket socket)
		{
			lock (SyncRoot)
			{
				Socket = socket;
				_remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
				CancellationTokenSource = new CancellationTokenSource();

				var args = new ThreadArgs(socket, CancellationTokenSource.Token);

				_readThread = new Thread(ReadLoop)
					{
						Name = string.Format("EndPoint '{0}' Socket Reading", Name),
						IsBackground = true,
					};
				_readThread.Start(args);

				_writeThread = new Thread(WriteLoop)
					{
						Name = string.Format("EndPoint '{0}' Socket Writing", Name),
						IsBackground = true,
					};
				_writeThread.Start(args);

				Log.InfoFormat("{0}: Connected to {1}", Name, _remoteEndPoint);
			}
		}

	}
}