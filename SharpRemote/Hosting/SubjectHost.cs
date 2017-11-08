using System;
using System.Collections.Generic;
using SharpRemote.CodeGeneration;
using SharpRemote.Extensions;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// <see cref="ISubjectHost"/> implementation that uses an <see cref="Activator"/> to
	/// create object instances.
	/// </summary>
	internal sealed class SubjectHost
		: ISubjectHost
	{
		private readonly ITypeResolver _customTypeResolver;
		private readonly IRemotingEndPoint _endpoint;
		private readonly Dictionary<ulong, object> _subjects;
		private readonly Dictionary<ulong, IServant> _servants;
		private readonly object _syncRoot;
		private readonly DefaultImplementationRegistry _registry;
		private readonly Action _onDisposed;

		private bool _isDisposed;

		private Type GetType(string assemblyQualifiedName)
		{
			if (_customTypeResolver != null)
				return _customTypeResolver.GetType(assemblyQualifiedName);

			return TypeResolver.GetType(assemblyQualifiedName);
		}

		public SubjectHost(IRemotingEndPoint endpoint,
			DefaultImplementationRegistry registry,
			Action onDisposed = null,
			ITypeResolver customTypeResolver = null)
		{
			if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
			if (registry == null) throw new ArgumentNullException(nameof(registry));

			_registry = registry;
			_customTypeResolver = customTypeResolver;
			_endpoint = endpoint;
			_onDisposed = onDisposed;
			_syncRoot = new object();
			_servants = new Dictionary<ulong, IServant>();
			_subjects = new Dictionary<ulong, object>();
		}

		public void CreateSubject3(ulong objectId, Type interfaceType)
		{
			var type = _registry.GetImplementation(interfaceType);
			CreateSubject1(objectId, type, interfaceType);
		}

		public void RegisterDefaultImplementation(Type implementation, Type interfaceType)
		{
			_registry.RegisterDefaultImplementation(implementation, interfaceType);
		}

		public void CreateSubject1(ulong objectId, Type type, Type interfaceType)
		{
			var subject = Activator.CreateInstance(type);

			lock (_syncRoot)
			{
				_subjects.Add(objectId, subject);
				var method = typeof(IRemotingEndPoint).GetMethod("CreateServant").MakeGenericMethod(interfaceType);
				var servant = (IServant)method.Invoke(_endpoint, new[] { objectId, subject });
				_servants.Add(objectId, servant);
			}
		}

		public void CreateSubject2(ulong objectId, string assemblyQualifiedTypeName, Type interfaceType)
		{
			var type = GetType(assemblyQualifiedTypeName);
			CreateSubject1(objectId, type, interfaceType);
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				if (_isDisposed)
					return;

				_subjects.Clear();

				// TODO: Remove / dispose all subjects...
				foreach (var subject in _servants.Values)
				{
					var disp = subject.Subject as IDisposable;
					if (disp != null)
						disp.TryDispose();
				}
				_servants.Clear();

				if (_onDisposed != null)
					_onDisposed();

				_isDisposed = true;
			}
		}
	}
}