﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.FaultTolerance.Fallback;

namespace SharpRemote.CodeGeneration.FaultTolerance
{
	internal sealed class ProxyTypeStorage
	{
		private readonly Dictionary<Type, IFallbackProxyCreator> _fallbackProxyCreators;
		private readonly Dictionary<Type, IDefaultFallbackCreator> _fallbackCreators;
		private readonly Dictionary<Type, ITimeoutProxyCreator> _timeoutCreators;
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
			_timeoutCreators = new Dictionary<Type, ITimeoutProxyCreator>();
		}

		public T CreateProxyWithFallback<T>(T subject, T fallback)
		{
			IFallbackProxyCreator creator;
			lock (_syncRoot)
			{
				var type = typeof(T);
				if (!_fallbackProxyCreators.TryGetValue(type, out creator))
				{
					var description = _typeModel.Add(type, assumeByReference: true);
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
					var description = _typeModel.Add(type, assumeByReference: true);
					creator = new DefaultFallbackCreator<T>(_moduleBuilder, description);
					_fallbackCreators.Add(type, creator);
				}
			}
			return (T)creator.Create();
		}

		public T CreateProxyWithTimeout<T>(T subject, TimeSpan maximumMethodLatency) where T : class
		{
			ITimeoutProxyCreator creator;
			lock (_syncRoot)
			{
				var type = typeof(T);
				if (!_timeoutCreators.TryGetValue(type, out creator))
				{
					var description = _typeModel.Add(type, assumeByReference: true);
					creator = new TimeoutProxyCreator<T>(_moduleBuilder, description);
					_timeoutCreators.Add(type, creator);
				}
			}
			return (T)creator.Create(subject, maximumMethodLatency);
		}
	}
}
