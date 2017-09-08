using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	///     Describes a service that was located using a <see cref="NetworkServiceDiscoverer" />.
	/// </summary>
	public struct Service : IEquatable<Service>
	{
		private readonly IPEndPoint _endPoint;
		private readonly IPAddress _localAddress;
		private readonly string _name;
		private readonly string _networkInterfaceId;
		private readonly string _payload;

		internal Service(string name, IPEndPoint ep, IPAddress localAddress, string networkInterfaceId = null, string payload = null)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (ep == null) throw new ArgumentNullException(nameof(ep));
			if (localAddress == null) throw new ArgumentNullException(nameof(localAddress));

			_name = name;
			_endPoint = ep;
			_localAddress = localAddress;
			_networkInterfaceId = networkInterfaceId;
			_payload = payload;
		}

		/// <summary>
		///     The name of the service.
		/// </summary>
		public string Name => _name;

		/// <summary>
		///     The endpoint the service is bound to.
		/// </summary>
		public IPEndPoint EndPoint => _endPoint;

		/// <summary>
		///     The IP-Address of the adapter that received the answer from the service.
		/// </summary>
		public IPAddress LocalAddress => _localAddress;

		/// <summary>
		///     The <see cref="NetworkInterface.Id" /> of the <see cref="NetworkInterface" />
		///     that received the response.
		/// </summary>
		public string NetworkInterfaceId => _networkInterfaceId;

		/// <summary>
		///     The payload that was added to the service entry via <see cref="INetworkServiceDiscoverer.RegisterService"/>.
		/// </summary>
		public string Payload => _payload;

		/// <inheritdoc />
		public bool Equals(Service other)
		{
			return string.Equals(_name, other._name) &&
			       Equals(_endPoint, other._endPoint) &&
			       Equals(_localAddress, other._localAddress) &&
			       Equals(_payload, other._payload);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Service && Equals((Service) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				return ((_name.GetHashCode())*397) ^
				       _endPoint.GetHashCode() ^
				       _localAddress.GetHashCode();
			}
		}

		public static bool operator ==(Service left, Service right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Service left, Service right)
		{
			return !left.Equals(right);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendFormat("{0}@{1} via {2}", _name, _endPoint, _localAddress);
			if (_payload != null)
			{
				builder.AppendFormat(", {0}", _payload);
			}
			return builder.ToString();
		}
	}
}