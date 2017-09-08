using System;
using System.Net;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	///     Represents a registered network service that can be found
	///     via <see cref="NetworkServiceDiscoverer.FindServices(string)" />.
	/// </summary>
	public sealed class RegisteredService
		: IDisposable
	{
		internal RegisteredService(string name, IPEndPoint endPoint, string payload)
		{
			Name = name;
			EndPoint = endPoint;
			Payload = payload;
		}

		/// <summary>
		///     The name of the registered service.
		/// </summary>
		public string Name { get; }

		/// <summary>
		///     The endpoint the registered service operates on.
		/// </summary>
		public IPEndPoint EndPoint { get; }

		/// <summary>
		///     An optional payload that better describes the service.
		/// </summary>
		public string Payload { get; }

		/// <inheritdoc />
		public void Dispose()
		{
			OnDisposed?.Invoke(this);
		}

		private bool Equals(RegisteredService other)
		{
			return string.Equals(Name, other.Name) && EndPoint.Equals(other.EndPoint);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(objA: null, objB: obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is RegisteredService && Equals((RegisteredService) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				return (Name.GetHashCode() * 397) ^ EndPoint.GetHashCode();
			}
		}

		internal event Action<RegisteredService> OnDisposed;
	}
}