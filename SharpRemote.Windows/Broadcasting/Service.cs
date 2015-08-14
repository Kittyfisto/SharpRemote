using System;
using System.Net;

namespace SharpRemote.Broadcasting
{
	internal struct Service : IEquatable<Service>
	{
		private readonly IPEndPoint _endPoint;
		private readonly string _name;

		public Service(string name, IPEndPoint ep)
		{
			_name = name;
			_endPoint = ep;
		}

		public string Name
		{
			get { return _name; }
		}

		public IPEndPoint EndPoint
		{
			get { return _endPoint; }
		}

		public bool Equals(Service other)
		{
			return string.Equals(_name, other._name) && Equals(_endPoint, other._endPoint);
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
				return ((_name != null ? _name.GetHashCode() : 0)*397) ^ (_endPoint != null ? _endPoint.GetHashCode() : 0);
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
	}
}