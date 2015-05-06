using System;
using System.Collections.Generic;
using SharpRemote.Hosting;

namespace SharpRemote.Host
{
	internal sealed class SubjectHost
		: ISubjectHost
	{
		private readonly IRemotingEndPoint _endpoint;
		private readonly Dictionary<ulong, object> _subjects;
		private ulong _nextServantId;
		private readonly Action _disposed;
		private bool _isDisposed;

		public SubjectHost(IRemotingEndPoint endpoint, ulong firstServantId, Action disposed)
		{
			if (endpoint == null) throw new ArgumentNullException("endpoint");
			if (disposed == null) throw new ArgumentNullException("disposed");

			_endpoint = endpoint;
			_nextServantId = firstServantId;
			_disposed = disposed;
			_subjects = new Dictionary<ulong, object>();
		}

		public ulong CreateSubject(Type type, Type interfaceType)
		{
			var servantId = _nextServantId++;
			var subject = Activator.CreateInstance(type);
			var method = typeof (IRemotingEndPoint).GetMethod("CreateServant").MakeGenericMethod(interfaceType);
			var servant = (IServant)method.Invoke(_endpoint, new []{servantId, subject});
			_subjects.Add(servantId, subject);
			return servantId;
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			// TODO: Remove / dispose all subjects...

			_disposed();
			_isDisposed = true;
		}
	}
}