using System;

namespace SharpRemote.CodeGeneration.FaultTolerance
{
	internal sealed class ProxyFactory<T>
		: IProxyFactory<T>
		where T : class
	{
		private readonly ProxyTypeStorage _storage;
		private readonly T _subject;

		public ProxyFactory(ProxyTypeStorage storage, T subject)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));
			if (subject == null)
				throw new ArgumentNullException(nameof(subject));

			_storage = storage;
			_subject = subject;
		}

		public IProxyFactory<T> WithMaximumLatencyOf(TimeSpan maximumMethodLatency)
		{
			throw new NotImplementedException();
		}

		public IProxyFactory<T> WithDefaultFallback()
		{
			var fallback = _storage.CreateDefaultFallback<T>();
			return WithFallbackTo(fallback);
		}

		public IProxyFactory<T> WithFallbackTo(T fallback)
		{
			if (fallback == null)
				throw new ArgumentNullException(nameof(fallback));

			var proxy = _storage.CreateProxyWithFallback(_subject, fallback);
			return new ProxyFactory<T>(_storage, (T) proxy);
		}

		public IProxyFactory<T> WithMaximumRetries(int numberOfRetries)
		{
			throw new NotImplementedException();
		}

		public T Create()
		{
			return _subject;
		}
	}
}