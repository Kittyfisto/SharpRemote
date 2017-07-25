using System;
using System.Net;
using SharpRemote.CodeGeneration;
using SharpRemote.Extensions;

// ReSharper disable CheckNamespace
namespace SharpRemote.Hosting
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// Silo implementation not meant for production code as it uses the entire remoting chain without an actual need for it.
	/// </summary>
	public sealed class InProcessRemotingSilo
		: ISilo
	{
		private readonly ITypeResolver _customTypeResolver;
		private readonly SocketRemotingEndPointClient _client;
		private readonly SocketRemotingEndPointServer _server;
		private readonly ISubjectHost _subjectHostProxy;
		private readonly SubjectHost _subjectHost;
		private readonly DefaultImplementationRegistry _registry;
		private readonly object _syncRoot;
		private bool _isDisposed;
		private ulong _nextObjectId;

		private Type GetType(string assemblyQualifiedName)
		{
			if (_customTypeResolver != null)
				return _customTypeResolver.GetType(assemblyQualifiedName);

			return TypeResolver.GetType(assemblyQualifiedName);
		}

		/// <summary>
		/// Initializes a new silo that hosts all objects in the same process it is used in, but with
		/// proxy and servant implementations in between.
		/// Is only really useful for debugging remoting.
		/// </summary>
		/// <param name="customTypeResolver">The type resolver, if any, that is used to resolve types by their assembly qualified name</param>
		public InProcessRemotingSilo(ITypeResolver customTypeResolver = null)
		{
			_customTypeResolver = customTypeResolver;
			const int subjectHostId = 0;
			_nextObjectId = subjectHostId + 1;

			_client = new SocketRemotingEndPointClient();
			_subjectHostProxy = _client.CreateProxy<ISubjectHost>(subjectHostId);

			_syncRoot = new object();

			_server = new SocketRemotingEndPointServer();
			_registry = new DefaultImplementationRegistry();
			_subjectHost = new SubjectHost(_server, _registry);
			_server.CreateServant(subjectHostId, (ISubjectHost)_subjectHost);
			_server.Bind(IPAddress.Loopback);

			_client.Connect(_server.LocalEndPoint, TimeSpan.FromSeconds(5));
		}

		public bool IsDisposed
		{
			get { return _isDisposed; }
		}

		public void RegisterDefaultImplementation<TInterface, TImplementation>() where TInterface : class where TImplementation : TInterface
		{
			_registry.RegisterDefaultImplementation(typeof(TImplementation), typeof(TInterface));
		}

		public TInterface CreateGrain<TInterface>(params object[] parameters) where TInterface : class
		{
			var type = _registry.GetImplementation(typeof (TInterface));
			return CreateGrain<TInterface>(type, parameters);
		}

		public TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName, params object[] parameters) where TInterface : class
		{
			var type = GetType(assemblyQualifiedTypeName);
			return CreateGrain<TInterface>(type, parameters);
		}

		public TInterface CreateGrain<TInterface>(Type implementation, params object[] parameters) where TInterface : class
		{
			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			_subjectHostProxy.CreateSubject1(objectId, implementation, typeof (TInterface));
			var proxy = _client.CreateProxy<TInterface>(objectId);
			return proxy;
		}

		public TInterface CreateGrain<TInterface, TImplementation>(params object[] parameters) where TInterface : class where TImplementation : TInterface
		{
			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			_subjectHostProxy.CreateSubject1(objectId, typeof(TImplementation), typeof(TInterface));
			var proxy = _client.CreateProxy<TInterface>(objectId);
			return proxy;
		}

		public void Dispose()
		{
			_subjectHostProxy.TryDispose();
			_client.Dispose();
			_server.Dispose();
			_isDisposed = true;
		}
	}
}