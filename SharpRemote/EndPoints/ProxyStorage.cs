using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using SharpRemote.CodeGeneration;

namespace SharpRemote.EndPoints
{
	internal sealed class ProxyStorage
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly ICodeGenerator _codeGenerator;
		private readonly IEndPointChannel _endPointChannel;
		private readonly Dictionary<ulong, WeakReference<IProxy>> _proxiesById;
		private readonly IRemotingEndPoint _remotingEndPoint;

		private readonly object _syncRoot;
		private int _numProxiesCollected;

		public ProxyStorage(IRemotingEndPoint remotingEndPoint,
		                    IEndPointChannel endPointChannel,
		                    ICodeGenerator codeGenerator)
		{
			if (remotingEndPoint == null)
				throw new ArgumentNullException(nameof(remotingEndPoint));
			if (endPointChannel == null)
				throw new ArgumentNullException(nameof(endPointChannel));
			if (codeGenerator == null)
				throw new ArgumentNullException(nameof(codeGenerator));

			_remotingEndPoint = remotingEndPoint;
			_endPointChannel = endPointChannel;
			_codeGenerator = codeGenerator;
			_syncRoot = new object();
			_proxiesById = new Dictionary<ulong, WeakReference<IProxy>>();
		}

		/// <summary>
		///     Returns all the proxies of this endpoint.
		///     Used for testing.
		/// </summary>
		public IEnumerable<IProxy> Proxies
		{
			get
			{
				lock (_syncRoot)
				{
					var aliveProxies = new List<IProxy>();

					foreach (var pair in _proxiesById)
					{
						IProxy proxy;
						if (pair.Value.TryGetTarget(out proxy)) aliveProxies.Add(proxy);
					}

					return aliveProxies;
				}
			}
		}

		public int NumProxiesCollected => _numProxiesCollected;

		public T CreateProxy<T>(ulong objectId) where T : class
		{
			lock (_syncRoot)
			{
				if (Log.IsDebugEnabled)
					Log.DebugFormat("{0}: Adding proxy '#{1}' of type '{2}'", _remotingEndPoint.Name, objectId, typeof(T).FullName);

				var proxy = _codeGenerator.CreateProxy<T>(_remotingEndPoint, _endPointChannel, objectId);
				var grain = new WeakReference<IProxy>((IProxy) proxy);
				_proxiesById.Add(objectId, grain);
				return proxy;
			}
		}

		public T GetProxy<T>(ulong objectId) where T : class
		{
			if (Log.IsDebugEnabled)
				Log.DebugFormat("{0}: Retrieving proxy '#{1}' of type '{2}'", _remotingEndPoint.Name, objectId, typeof(T).FullName);

			IProxy proxy;
			lock (_syncRoot)
			{
				WeakReference<IProxy> grain;
				if (!_proxiesById.TryGetValue(objectId, out grain) || !grain.TryGetTarget(out proxy))
					throw new ArgumentException(string.Format("No such proxy: {0}", objectId));
			}

			if (!(proxy is T))
				throw new ArgumentException(string.Format("The proxy '{0}', {1} is not related to interface: {2}",
				                                          objectId,
				                                          proxy.GetType().Name,
				                                          typeof(T).Name));

			return (T) proxy;
		}

		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			lock (_syncRoot)
			{
				IProxy proxy;
				WeakReference<IProxy> grain;
				if (!_proxiesById.TryGetValue(objectId, out grain))
				{
					// If the proxy doesn't exist, then we can simply create a new one...
					var value = _codeGenerator.CreateProxy<T>(_remotingEndPoint, _endPointChannel, objectId);
					grain = new WeakReference<IProxy>((IProxy) value);
					_proxiesById.Add(objectId, grain);
					return value;
				}

				if (!grain.TryGetTarget(out proxy))
				{
					// It's possible that the proxy did exist at one point, then was collected by the GC, but
					// our internal GC didn't have the time to remove that proxy from the dictionary yet, which
					// means that we have to *replace* the existing weak-reference with a new, living one
					var value = _codeGenerator.CreateProxy<T>(_remotingEndPoint, _endPointChannel, objectId);
					grain = new WeakReference<IProxy>(proxy);
					_proxiesById[objectId] = grain;
					return value;
				}

				return (T) proxy;
			}
		}

		public int RemoveUnusedProxies()
		{
			lock (_syncRoot)
			{
				List<ulong> toRemove = null;

				foreach (var pair in _proxiesById)
				{
					IProxy proxy;
					if (!pair.Value.TryGetTarget(out proxy))
					{
						if (toRemove == null)
							toRemove = new List<ulong>();

						toRemove.Add(pair.Key);
					}
				}

				if (toRemove != null)
				{
					foreach (var key in toRemove)
					{
						if (Log.IsDebugEnabled)
							Log.DebugFormat("{0}: Removing proxy '#{1}' from list of available proxies because it is no longer reachable (it has been garbage collected)",
							                _remotingEndPoint.Name,
							                key);

						_proxiesById.Remove(key);
					}

					_numProxiesCollected += toRemove.Count;
					return toRemove.Count;
				}

				return 0;
			}
		}

		public void TryGetProxy(ulong servantId, out IProxy proxy, out int numProxies)
		{
			lock (_syncRoot)
			{
				numProxies = _proxiesById.Count;
				WeakReference<IProxy> grain;
				if (_proxiesById.TryGetValue(servantId, out grain))
					grain.TryGetTarget(out proxy);
				else
					proxy = null;
			}
		}
	}
}