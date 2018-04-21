using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.FaultTolerance.Fallback;

namespace SharpRemote.CodeGeneration.FaultTolerance
{
	internal sealed class ProxyTypeStorage
	{
		private readonly Dictionary<Type, IFallbackProxyCreator> _fallbackProxyCreators;
		private readonly Dictionary<Type, IDefaultFallbackCreator> _fallbackCreators;
		private readonly object _syncRoot;
		private readonly TypeModel _typeModel;
		private readonly ModuleBuilder _moduleBuilder;

		public ProxyTypeStorage(ModuleBuilder moduleBuilder)
		{
			_moduleBuilder = moduleBuilder;
			_syncRoot = new object();
			_typeModel = new TypeModel();
			_fallbackProxyCreators = new Dictionary<Type, IFallbackProxyCreator>();
			_fallbackCreators = new Dictionary<Type, IDefaultFallbackCreator>();
		}

		public T CreateProxyWithFallback<T>(T subject, T fallback)
		{
			IFallbackProxyCreator creator;
			lock (_syncRoot)
			{
				var type = typeof(T);
				if (!_fallbackProxyCreators.TryGetValue(type, out creator))
				{
					var description = _typeModel.Add(type, assumeProxy: true);
					creator = new FallbackProxyCreator<T>(_moduleBuilder, description);
					_fallbackProxyCreators.Add(typeof(T), creator);
				}
			}
			return (T)creator.Create(subject, fallback);
		}

		public T CreateDefaultFallback<T>()
		{
			IDefaultFallbackCreator creator;
			lock (_syncRoot)
			{
				var type = typeof(T);
				if (!_fallbackCreators.TryGetValue(type, out creator))
				{
					var description = _typeModel.Add(type, assumeProxy: true);
					creator = new DefaultFallbackCreator<T>(_moduleBuilder, description);
					_fallbackCreators.Add(typeof(T), creator);
				}
			}
			return (T)creator.Create();
		}
	}
}
