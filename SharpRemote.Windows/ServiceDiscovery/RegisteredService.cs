using System;
using System.Net;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	///     Represents a registered network service that can be found
	///     via <see cref="NetworkServiceDiscoverer.FindServices" />.
	/// </summary>
	public sealed class RegisteredService
		: IDisposable
	{
		private readonly IPEndPoint _endPoint;
		private readonly string _name;

		internal RegisteredService(string name, IPEndPoint endPoint)
		{
			_name = name;
			_endPoint = endPoint;
		}

		/// <summary>
		///     The name of the registered service.
		/// </summary>
		public string Name
		{
			get { return _name; }
		}

		/// <summary>
		///     The endpoint the registered service operates on.
		/// </summary>
		public IPEndPoint EndPoint
		{
			get { return _endPoint; }
		}

		public void Dispose()
		{
			Action<RegisteredService> fn = OnDisposed;
			if (fn != null)
				fn(this);
		}

		private bool Equals(RegisteredService other)
		{
			return string.Equals(_name, other._name) && _endPoint.Equals(other._endPoint);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is RegisteredService && Equals((RegisteredService) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (_name.GetHashCode()*397) ^ _endPoint.GetHashCode();
			}
		}

		internal event Action<RegisteredService> OnDisposed;
	}
}