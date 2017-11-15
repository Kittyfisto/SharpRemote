using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using log4net;
using SharpRemote.CodeGeneration;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class NamedPipeRemotingEndPointClient
		: AbstractNamedPipeEndPoint<NamedPipeClientStream>
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="clientAuthenticator"></param>
		/// <param name="serverAuthenticator"></param>
		/// <param name="codeGenerator"></param>
		/// <param name="heartbeatSettings"></param>
		/// <param name="latencySettings"></param>
		/// <param name="endPointSettings"></param>
		public NamedPipeRemotingEndPointClient(string name = null,
		                                       IAuthenticator clientAuthenticator = null,
											   IAuthenticator serverAuthenticator = null,
											   ICodeGenerator codeGenerator = null,
											   HeartbeatSettings heartbeatSettings = null,
											   LatencySettings latencySettings = null,
											   EndPointSettings endPointSettings = null)
			: base(name, EndPointType.Client,
			       clientAuthenticator,
			       serverAuthenticator,
			       codeGenerator,
			       heartbeatSettings,
			       latencySettings,
			       endPointSettings)
		{
		}

		protected override void DisposeAdditional()
		{
			
		}

		protected override void DisconnectTransport(NamedPipeClientStream socket, bool reuseSocket)
		{
			socket.Dispose();
		}

		protected override void DisposeAfterDisconnect(NamedPipeClientStream socket)
		{
			socket.TryDispose();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout"></param>
		public ConnectionId Connect(NamedPipeEndPoint endPoint, TimeSpan timeout)
		{
			Exception exception;
			ConnectionId connectionId;
			if (!TryConnect(endPoint, timeout, out exception, out connectionId))
				throw exception;

			return connectionId;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout"></param>
		/// <param name="exception"></param>
		/// <param name="connectionId"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public bool TryConnect(NamedPipeEndPoint endPoint, TimeSpan timeout, out Exception exception, out ConnectionId connectionId)
		{
			if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));
			if (Equals(endPoint, LocalEndPoint))
				throw new ArgumentException("An endPoint cannot be connected to itself", nameof(endPoint));
			if (endPoint.Type != NamedPipeEndPoint.PipeType.Server)
				throw new ArgumentException("An endpoint can only establish a connection with a server-side enpoint", nameof(endPoint));
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
			if (IsConnected)
				throw new InvalidOperationException(
					"This endPoint is already connected to another endPoint and cannot establish any more connections");

			bool success = false;
			NamedPipeClientStream pipe = null;

			try
			{
				var started = DateTime.Now;
				pipe = new NamedPipeClientStream(Localhost, endPoint.PipeName);
				try
				{
					var totalMilliseconds = (int) Math.Min(int.MaxValue, timeout.TotalMilliseconds);
					pipe.Connect(totalMilliseconds);
				}
				catch (TimeoutException e)
				{
					exception = new NoSuchNamedPipeEndPointException(endPoint, timeout, e);
					connectionId = ConnectionId.None;
					return false;
				}
				catch (IOException e)
				{
					exception = new NoSuchNamedPipeEndPointException(endPoint, timeout, e);
					connectionId = ConnectionId.None;
					return false;
				}
				catch (Exception e)
				{
					Log.ErrorFormat("Cauhgt unexpected exception while connecting to '{0}': {1}", endPoint, e);

					exception = new NoSuchNamedPipeEndPointException(endPoint, timeout, e);
					connectionId = ConnectionId.None;
					return false;
				}

				var remaining = timeout - (DateTime.Now - started);
				ErrorType errorType;
				string error;
				if (!TryPerformOutgoingHandshake(pipe, remaining, out errorType, out error, out connectionId))
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
					CurrentConnectionId = connectionId;
					return false;
				}

				InternalRemoteEndPoint = endPoint;
				LocalEndPoint = NamedPipeEndPoint.FromClient(Name);

				Log.InfoFormat("EndPoint '{0}' successfully connected to '{1}'", Name, endPoint);

				FireOnConnected(endPoint, CurrentConnectionId);

				success = true;
				exception = null;
				return true;
			}
			finally
			{
				if (!success)
				{
					if (pipe != null)
					{
						pipe.Close();
						pipe.Dispose();
					}

					InternalRemoteEndPoint = null;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		public ConnectionId Connect(NamedPipeEndPoint endPoint)
		{
			return Connect(endPoint, TimeSpan.FromSeconds(1));
		}
	}
}