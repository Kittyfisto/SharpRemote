using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using SharpRemote.Exceptions;
using SharpRemote.ServiceDiscovery;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class SocketRemotingEndPointClient
		: AbstractIPSocketRemotingEndPoint
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly NetworkServiceDiscoverer _networkServiceDiscoverer;

		/// <summary>
		///     Creates a new socket end point that (optionally) is bound to the given
		///     P2P name, if PNRP is available, otherwise the name is only used for debugging.
		/// </summary>
		/// <remarks>
		///     Currently, no exception is thrown when the required P2P service "PNRPsvc" is
		///     not installed or not running. Check the <see cref="AbstractIPSocketRemotingEndPoint.IsP2PAvailable" /> flag to
		///     find out if it is.
		/// </remarks>
		/// <param name="name">The name of this socket, used to publish it via PNRP as well as to refer to this endpoint in diagnostic output</param>
		/// <param name="clientAuthenticator">The authenticator, if any, to authenticate a client against a server (both need to use the same authenticator)</param>
		/// <param name="serverAuthenticator">The authenticator, if any, to authenticate a server against a client (both need to use the same authenticator)</param>
		/// <param name="customTypeResolver">The type resolver, if any, responsible for resolving Type objects by their assembly qualified name</param>
		/// <param name="networkServiceDiscoverer">The discoverer used to find services by name within the local network</param>
		/// <param name="serializer">The serializer used serialize and deserialize values - if none is specified a new one is created</param>
		/// <param name="heartbeatSettings">The settings for heartbeat mechanism, if none are specified, then default settings are used</param>
		/// <param name="latencySettings">The settings for latency measurements, if none are specified, then default settings are used</param>
		/// <param name="endPointSettings">The settings for the endpoint itself (max. number of concurrent calls, etc...)</param>
		public SocketRemotingEndPointClient(string name = null,
		                                    IAuthenticator clientAuthenticator = null,
		                                    IAuthenticator serverAuthenticator = null,
		                                    ITypeResolver customTypeResolver = null,
		                                    NetworkServiceDiscoverer networkServiceDiscoverer = null,
		                                    Serializer serializer = null,
		                                    HeartbeatSettings heartbeatSettings = null,
		                                    LatencySettings latencySettings = null,
		                                    EndPointSettings endPointSettings = null)
			: base(EndPointType.Client,
			       name,
			       clientAuthenticator,
			       serverAuthenticator,
			       customTypeResolver,
			       serializer,
			       heartbeatSettings,
			       latencySettings,
			       endPointSettings)
		{
			_networkServiceDiscoverer = networkServiceDiscoverer;
		}

		/// <summary>
		///     Tries to connect to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <returns>True when a connection could be established, false otherwise</returns>
		public bool TryConnect(string endPointName)
		{
			return TryConnect(endPointName, TimeSpan.FromSeconds(1));
		}

		/// <summary>
		///     Tries to connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="timeout"></param>
		/// <returns>True when the connection succeeded, false otherwise</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="endPointName"/> is null</exception>
		/// <exception cref="ArgumentException">When <paramref name="endPointName"/> is empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">When <paramref name="timeout"/> is less or equal to <see cref="TimeSpan.Zero"/></exception>
		/// <exception cref="InvalidOperationException">When no network service discoverer was specified when creating this client</exception>
		public bool TryConnect(string endPointName, TimeSpan timeout)
		{
			Exception unused;
			return TryConnect(endPointName, timeout, out unused);
		}

		/// <summary>
		///     Tries to connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <returns>True when the connection succeeded, false otherwise</returns>
		public bool TryConnect(IPEndPoint endPoint)
		{
			return TryConnect(endPoint, TimeSpan.FromSeconds(1));
		}

		/// <summary>
		///     Tries to connect this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">The amount of time this method should block and await a successful connection from the remote end-point</param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		public bool TryConnect(IPEndPoint endPoint, TimeSpan timeout)
		{
			if (endPoint == null) throw new ArgumentNullException("endPoint");
			if (Equals(endPoint, LocalEndPoint))
				throw new ArgumentException("An endPoint cannot be connected to itself", "endPoint");
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeout");
			if (IsConnected)
				throw new InvalidOperationException(
					"This endPoint is already connected to another endPoint and cannot establish any more connections");

			Exception unused;
			return TryConnect(endPoint, timeout, out unused);
		}

		private bool TryConnect(string endPointName, TimeSpan timeout, out Exception exception)
		{
			if (endPointName == null) throw new ArgumentNullException("endPointName");
			if (endPointName == "") throw new ArgumentException("endPointName");
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeout");
			if (_networkServiceDiscoverer == null) throw new InvalidOperationException("No discoverer was specified when creating this client and thus network service discovery by name is not possible");

			var results = _networkServiceDiscoverer.FindServices(endPointName);
			if (results.Count == 0)
			{
				exception = new NoSuchIPEndPointException(endPointName);
				return false;
			}

			foreach (var result in results)
			{
				if (TryConnect(result.EndPoint, timeout, out exception))
					return true;
			}

			exception = new NoSuchIPEndPointException(endPointName);
			return false;
		}

		private bool TryConnect(IPEndPoint endPoint, TimeSpan timeout, out Exception exception)
		{
			if (endPoint == null) throw new ArgumentNullException("endPoint");
			if (Equals(endPoint, LocalEndPoint))
				throw new ArgumentException("An endPoint cannot be connected to itself", "endPoint");
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeout");
			if (IsConnected)
				throw new InvalidOperationException(
					"This endPoint is already connected to another endPoint and cannot establish any more connections");

			Log.DebugFormat("Trying to connect to '{0}', timeout: {1}ms", endPoint, timeout.TotalMilliseconds);

			bool success = false;
			Socket socket = null;
			try
			{
				DateTime started = DateTime.Now;
				var task = new Task<Exception>(() =>
				{
					try
					{
						Log.DebugFormat("Task to connect to '{0}' started", endPoint);

						socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

						Log.DebugFormat("Socket to connect to '{0}' created", endPoint);

						socket.Connect(endPoint);

						Log.DebugFormat("Socket connected to '{0}'", endPoint);

						return null;
					}
					catch (SocketException e)
					{
						return e;
					}
					catch (Exception e)
					{
						Log.WarnFormat("Caught unexpected exception while trying to connect to socket: {0}", e);
						return e;
					}
				}, TaskCreationOptions.LongRunning);
				task.Start();
				if (!task.Wait(timeout))
				{
					exception = new NoSuchIPEndPointException(endPoint, timeout);
					return false;
				}

				if (task.Result != null)
				{
					exception = new NoSuchIPEndPointException(endPoint, task.Result);
					return false;
				}

				TimeSpan remaining = timeout - (DateTime.Now - started);
				ErrorType errorType;
				string error;
				if (!TryPerformOutgoingHandshake(socket, remaining, out errorType, out error))
				{
					switch (errorType)
					{
						case ErrorType.Handshake:
							exception = new HandshakeException(error);
							break;

						case ErrorType.AuthenticationRequired:
							exception = new AuthenticationRequiredException(error);
							break;

						default:
							exception = new AuthenticationException(error);
							break;
					}
					return false;
				}

				RemoteEndPoint = endPoint;
				LocalEndPoint = (IPEndPoint)socket.LocalEndPoint;

				FireOnConnected(endPoint);

				success = true;
				exception = null;
				return true;
			}
			finally
			{
				if (!success)
				{
					if (socket != null)
					{
						socket.Close();
						socket.Dispose();
					}

					RemoteEndPoint = null;
				}
			}
		}

		/// <summary>
		///     Connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		public void Connect(string endPointName)
		{
			Connect(endPointName, TimeSpan.FromSeconds(1));
		}

		/// <summary>
		///     Connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="timeout"></param>
		/// <exception cref="ArgumentException">
		///     In case <paramref name="endPointName" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">When no such endPoint could be *found* - it might exist but this one is incapable of establishing a successfuly connection</exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="SocketRemotingEndPointServer" />
		///     - The given endPoint failed authentication
		/// </exception>
		public void Connect(string endPointName, TimeSpan timeout)
		{
			if (endPointName == null) throw new ArgumentNullException("endPointName");
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeout");
			if (_networkServiceDiscoverer == null) throw new InvalidOperationException("No discoverer was specified when creating this client and thus network service discovery by name is not possible");

			var results = _networkServiceDiscoverer.FindServices(endPointName);

			if (results.Count == 0)
			{
				throw new NoSuchIPEndPointException(endPointName);
			}

			foreach (var result in results)
			{
				Connect(result.EndPoint, timeout);
			}
		}

		/// <summary>
		///     Connects this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// ///
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">When no such endPoint could be *found* - it might exist but this one is incapable of establishing a successfuly connection</exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="SocketRemotingEndPointServer" />
		///     - The given endPoint failed authentication
		/// </exception>
		public void Connect(IPEndPoint endPoint)
		{
			Connect(endPoint, TimeSpan.FromSeconds(1));
		}

		/// <summary>
		///     Connects this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">The amount of time this method should block and await a successful connection from the remote end-point</param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">When no such endPoint could be *found* - it might exist but this one is incapable of establishing a successfuly connection</exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="SocketRemotingEndPointServer" />
		///     - The given endPoint failed authentication
		/// </exception>
		/// <exception cref="AuthenticationRequiredException">
		///     - The given endPoint requires authentication, but this one didn't provide any
		/// </exception>
		/// <exception cref="HandshakeException">
		///     - The handshake between this and the given endpoint failed
		/// </exception>
		public void Connect(IPEndPoint endPoint, TimeSpan timeout)
		{
			Exception e;
			if (!TryConnect(endPoint, timeout, out e))
				throw e;
		}

		protected override void DisposeAdditional()
		{
		}
	}
}