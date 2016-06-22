using System;
using System.Net;
using System.Net.NetworkInformation;

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

		internal Service(string name, IPEndPoint ep, IPAddress localAddress, string networkInterfaceId = null)
		{
			if (name == null) throw new ArgumentNullException("name");
			if (ep == null) throw new ArgumentNullException("ep");
			if (localAddress == null) throw new ArgumentNullException("localAddress");

			_name = name;
			_endPoint = ep;
			_localAddress = localAddress;
			_networkInterfaceId = null;
		}

		/// <summary>
		///     The name of the service.
		/// </summary>
		public string Name
		{
			get { return _name; }
		}

		/// <summary>
		///     The endpoint the service is bound to.
		/// </summary>
		public IPEndPoint EndPoint
		{
			get { return _endPoint; }
		}

		/// <summary>
		///     The IP-Address of the adapter that received the answer from the service.
		/// </summary>
		public IPAddress LocalAddress
		{
			get { return _localAddress; }
		}

		/// <summary>
		///     The <see cref="NetworkInterface.Id" /> of the <see cref="NetworkInterface" />
		///     that received the response.
		/// </summary>
		public string NetworkInterfaceId
		{
			get { return _networkInterfaceId; }
		}

		public bool Equals(Service other)
		{
			return string.Equals(_name, other._name) &&
			       Equals(_endPoint, other._endPoint) &&
			       Equals(_localAddress, other._localAddress);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Service && Equals((Service) obj);
		}

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

		public override string ToString()
		{
			return string.Format("{0}@{1} via {2}", _name, _endPoint, _localAddress);
		}
	}
}