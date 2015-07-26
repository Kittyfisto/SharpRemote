using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.PeerToPeer;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using SharpRemote.Exceptions;
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
		public SocketRemotingEndPointClient(string name = null,
		                              IAuthenticator clientAuthenticator = null,
		                              IAuthenticator serverAuthenticator = null,
		                              ITypeResolver customTypeResolver = null)
			: base(EndPointType.Client,
			name,
			clientAuthenticator,
			serverAuthenticator,
			customTypeResolver)
		{
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

			var resolver = new PeerNameResolver();
			PeerNameRecordCollection results = resolver.Resolve(new PeerName(endPointName, PeerNameType.Unsecured));

			if (results.Count == 0)
			{
				Log.ErrorFormat("Unable to find peer named '{0}'", endPointName);
				throw new NoSuchIPEndPointException(endPointName);
			}

			PeerNameRecord peer = results[0];
			IPEndPointCollection endPoints = peer.EndPointCollection;

			foreach (IPEndPoint ep in endPoints)
			{
				try
				{
					Connect(ep, timeout);
					break;
				}
				catch (NoSuchIPEndPointException) //< Let's try the next...
				{
				}
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
				var task = new Task(() =>
					{
						Log.DebugFormat("Task to connect to '{0}' started", endPoint);

						socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

						Log.DebugFormat("Socket to connect to '{0}' created", endPoint);

						socket.Connect(endPoint);

						Log.DebugFormat("Socket connected to '{0}'", endPoint);
					}, TaskCreationOptions.LongRunning);
				task.Start();
				if (!task.Wait(timeout))
					throw new NoSuchIPEndPointException(endPoint, timeout);

				TimeSpan remaining = timeout - (DateTime.Now - started);
				PerformOutgoingHandshake(socket, remaining);
				RemoteEndPoint = endPoint;
				LocalEndPoint = (IPEndPoint) socket.LocalEndPoint;

				success = true;
			}
			catch (AggregateException e)
			{
				ReadOnlyCollection<Exception> inner = e.InnerExceptions;
				if (inner.Count != 1)
					throw;

				Exception ex = inner[0];
				if (!(ex is SocketException))
					throw;

				throw new NoSuchIPEndPointException(endPoint, e);
			}
			catch (SocketException e)
			{
				throw new NoSuchIPEndPointException(endPoint, e);
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

		protected override void DisposeAdditional()
		{
		}
	}
}