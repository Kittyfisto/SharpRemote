using System;
using System.Net;
using SharpRemote.Extensions;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Silo implementation not meant for production code as it uses the entire remoting chain without an actual need for it.
	/// </summary>
	public sealed class InProcessRemotingSilo
		: ISilo
	{
		private readonly SocketRemotingEndPoint _localEndPoint;
		private readonly SocketRemotingEndPoint _remoteEndPoint;
		private readonly ISubjectHost _subjectHostProxy;
		private readonly SubjectHost _subjectHost;

		/// <summary>
		/// 
		/// </summary>
		public InProcessRemotingSilo()
		{
			const int subjectHostId = 0;

			_localEndPoint = new SocketRemotingEndPoint();
			_subjectHostProxy = _localEndPoint.CreateProxy<ISubjectHost>(subjectHostId);

			_remoteEndPoint = new SocketRemotingEndPoint();
			_subjectHost = new SubjectHost(_remoteEndPoint, subjectHostId + 1);
			_remoteEndPoint.CreateServant(subjectHostId, (ISubjectHost)_subjectHost);
			_remoteEndPoint.Bind(IPAddress.Loopback);

			_localEndPoint.Connect(_remoteEndPoint.LocalEndPoint, TimeSpan.FromSeconds(5));
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