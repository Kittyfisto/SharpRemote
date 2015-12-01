using System;

namespace SharpRemote
{
	/// <summary>
	/// Identifies a connection of one <see cref="SocketRemotingEndPointClient"/>.
	/// </summary>
	public struct ConnectionId
		: IEquatable<ConnectionId>
	{
		public static readonly ConnectionId None = new ConnectionId(0);

		public override string ToString()
		{
			if (_value == 0)
				return "None";

			return string.Format("Connection #{0}", _value);
		}

		public bool Equals(ConnectionId other)
		{
			return _value == other._value;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is ConnectionId && Equals((ConnectionId) obj);
		}

		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

		public static bool operator ==(ConnectionId left, ConnectionId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ConnectionId left, ConnectionId right)
		{
			return !left.Equals(right);
		}

		private readonly long _value;

		internal ConnectionId(long value)
		{
			_value = value;
		}
	}
}