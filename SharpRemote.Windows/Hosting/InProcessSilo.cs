using System;
using SharpRemote.CodeGeneration;

namespace SharpRemote.Hosting
{
	/// <summary>
	///     Hosts all objects in the calling process without any remoting overhead.
	///     The object returned is the object being created - not some proxy.
	/// </summary>
	public sealed class InProcessSilo
		: ISilo
	{
		private readonly ITypeResolver _customTypeResolver;
		private readonly DefaultImplementationRegistry _registry;
		private bool _isDisposed;

		/// <summary>
		///     Initializes a new silo that hosts objects in the very same process it is used from.
		/// </summary>
		/// <param name="customTypeResolver">The type resolver, if any, that is used to resolve types by their assembly qualified name</param>
		public InProcessSilo(ITypeResolver customTypeResolver = null)
		{
			_customTypeResolver = customTypeResolver;
			_registry = new DefaultImplementationRegistry();
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

		public TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName, params object[] parameters)
			where TInterface : class
		{
			Type type = GetType(assemblyQualifiedTypeName);
			return CreateGrain<TInterface>(type, parameters);
		}

		public TInterface CreateGrain<TInterface>(Type implementation, params object[] parameters) where TInterface : class
		{
			object subject = Activator.CreateInstance(implementation, parameters);
			return (TInterface) subject;
		}

		public TInterface CreateGrain<TInterface, TImplementation>(params object[] parameters) where TInterface : class where TImplementation : TInterface
		{
			object subject = Activator.CreateInstance(typeof(TImplementation), parameters);
			return (TInterface)subject;
		}

		public void Dispose()
		{
			_isDisposed = true;
		}

		private Type GetType(string assemblyQualifiedName)
		{
			if (_customTypeResolver != null)
				return _customTypeResolver.GetType(assemblyQualifiedName);

			return TypeResolver.GetType(assemblyQualifiedName);
		}
	}
}