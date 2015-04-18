using System;
using System.Net;
using SharpRemote.CodeGeneration;

namespace SharpRemote
{
	public sealed class PeerEndPoint
		: IEndPoint
	{
		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;
		private readonly EndPointChannel _channel;
		private readonly IPEndPoint _localAddress;

		public PeerEndPoint(IPEndPoint localAddress)
		{
			_localAddress = localAddress;

			_channel = new EndPointChannel();
			_proxyCreator = new ProxyCreator(_channel);
			_servantCreator = new ServantCreator();
		}

		public void Start()
		{
			
		}

		public IPEndPoint Address
		{
			get { return _localAddress; }
		}

		/// <summary>
		/// Creates a new object of type T.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		public T CreateProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <param name="subject"></param>
		/// <returns></returns>
		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="subject"></param>
		/// <returns></returns>
		public IServant CreateServant<T>(T subject) where T : class
		{
			return _servantCreator.CreateServant(1, subject);
		}

		public void Connect(IPEndPoint address)
		{
			
		}

		public void Dispose()
		{
			
		}
	}
}