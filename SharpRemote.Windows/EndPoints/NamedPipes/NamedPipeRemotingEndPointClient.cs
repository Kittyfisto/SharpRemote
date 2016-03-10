using System;
using System.IO;
using System.IO.Pipes;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class NamedPipeRemotingEndPointClient
		: AbstractNamedPipeEndPoint<NamedPipeClientStream>
	{
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
		public NamedPipeRemotingEndPointClient(string name = null,
		                                       IAuthenticator clientAuthenticator = null,
											   IAuthenticator serverAuthenticator = null,
											   ITypeResolver customTypeResolver = null,
											   Serializer serializer = null,
											   HeartbeatSettings heartbeatSettings = null,
											   LatencySettings latencySettings = null,
											   EndPointSettings endPointSettings = null)
			: base(name, EndPointType.Client,
			       clientAuthenticator,
			       serverAuthenticator,
			       customTypeResolver,
			       serializer,
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout"></param>
		/// <exception cref="NotImplementedException"></exception>
		public void Connect(NamedPipeEndPoint endPoint, TimeSpan timeout)
		{
			if (endPoint == null) throw new ArgumentNullException("endPoint");
			if (timeout <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException("timeout");

			var pipe = new NamedPipeClientStream(endPoint.PipeName, Name);
			try
			{
				pipe.Connect(timeout.Milliseconds);
			}
			catch (TimeoutException e)
			{
				throw new NoSuchNamedPipeEndPointException(endPoint, timeout, e);
			}
			catch (IOException e)
			{
				throw new NoSuchNamedPipeEndPointException(endPoint, timeout, e);
			}
		}
	}
}