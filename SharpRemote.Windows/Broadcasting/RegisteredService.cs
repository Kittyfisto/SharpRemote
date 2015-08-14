using System;
using System.Net;

namespace SharpRemote.Broadcasting
{
	internal sealed class RegisteredService
		: IDisposable
	{
		private readonly string _name;
		private readonly IPEndPoint _endPoint;

		public string Name
		{
			get { return _name; }
		}

		public IPEndPoint EndPoint
		{
			get { return _endPoint; }
		}

		public RegisteredService(string name, IPEndPoint ep)
		{
			_name = name;
			_endPoint = ep;
		}

		public event Action<RegisteredService> OnDisposed;

		public void Dispose()
		{
			var fn = OnDisposed;
			if (fn != null)
				fn(this);
		}
	}
}