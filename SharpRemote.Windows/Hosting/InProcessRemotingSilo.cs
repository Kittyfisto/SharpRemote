using System;
using System.Net;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Silo implementation not meant for production code as it uses the entire remoting chain without an actual need for it.
	/// </summary>
	public sealed class InProcessRemotingSilo
		: ISilo
	{
		private readonly SocketEndPoint _localEndPoint;
		private readonly SocketEndPoint _remoteEndPoint;
		private readonly ISubjectHost _subjectHostProxy;
		private readonly SubjectHost _subjectHost;

		public InProcessRemotingSilo()
		{
			const int subjectHostId = 0;

			_localEndPoint = new SocketEndPoint(IPAddress.Loopback);
			_subjectHostProxy = _localEndPoint.CreateProxy<ISubjectHost>(subjectHostId);

			_remoteEndPoint = new SocketEndPoint(IPAddress.Loopback);
			_subjectHost = new SubjectHost(_remoteEndPoint, subjectHostId + 1);
			_remoteEndPoint.CreateServant(subjectHostId, (ISubjectHost)_subjectHost);

			_localEndPoint.Connect(_remoteEndPoint.LocalAddress, TimeSpan.FromSeconds(1));
		}

		public TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName, params object[] parameters) where TInterface : class
		{
			return CreateGrain<TInterface>(Type.GetType(assemblyQualifiedTypeName), parameters);
		}

		public TInterface CreateGrain<TInterface>(Type implementation, params object[] parameters) where TInterface : class
		{
			var id = _subjectHostProxy.CreateSubject1(implementation, typeof (TInterface));
			var proxy = _localEndPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		public void Dispose()
		{
			_subjectHostProxy.TryDispose();
			_localEndPoint.Dispose();
			_remoteEndPoint.Dispose();
		}
	}
}