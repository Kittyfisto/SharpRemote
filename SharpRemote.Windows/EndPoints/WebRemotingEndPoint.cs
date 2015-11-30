using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SharpRemote.EndPoints
{
	internal sealed class WebRemotingEndPoint
		: AbstractEndPoint
		  , IRemotingEndPoint
		  , IEndPointChannel
	{
		public Task<MemoryStream> CallRemoteMethodAsync(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			throw new NotImplementedException();
		}

		public MemoryStream CallRemoteMethod(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public string Name
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsConnected
		{
			get { throw new NotImplementedException(); }
		}

		public ConnectionId CurrentConnectionId
		{
			get { throw new NotImplementedException(); }
		}

		public TimeSpan RoundtripTime
		{
			get { throw new NotImplementedException(); }
		}

		public EndPoint LocalEndPoint
		{
			get { throw new NotImplementedException(); }
		}

		public EndPoint RemoteEndPoint
		{
			get { throw new NotImplementedException(); }
		}

#pragma warning disable 67
		public event Action<EndPoint> OnConnected;
		public event Action<EndPoint> OnDisconnected;
		public event Action<EndPointDisconnectReason, ConnectionId> OnFailure;
#pragma warning restore 67

		public void Disconnect()
		{
			throw new NotImplementedException();
		}

		public T CreateProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public T GetProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			throw new NotImplementedException();
		}

		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			throw new NotImplementedException();
		}
	}
}