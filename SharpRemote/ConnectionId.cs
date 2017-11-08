using System;

namespace SharpRemote
{
	/// <summary>
	///     Identifies a connection of one <see cref="SocketEndPoint" />.
	/// </summary>
	public struct ConnectionId
		: IEquatable<ConnectionId>
	{
		/// <summary>
		///     The ConnectionId value used to represent "no" connection, i.e. the value returned by
		///     <see cref="IRemotingEndPoint.CurrentConnectionId" />
		///     if the endpoint is just not connected.
		/// </summary>
		public static readonly ConnectionId None = new ConnectionId(0);

		/// <inheritdoc />
		public override string ToString()
		{
			if (_value == 0)
				return "None";

			return string.Format("Connection #{0}", _value);
		}

		/// <summary>
		///     Compares this and the given ConnectionId for equality.
		///     Two ids are equal if they represent the same connection.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(ConnectionId other)
		{
			return _value == other._value;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is ConnectionId && Equals((ConnectionId) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

		/// <inheritdoc />
		public static bool operator ==(ConnectionId left, ConnectionId right)
		{
			return left.Equals(right);
		}

		/// <inheritdoc />
		public static bool operator !=(ConnectionId left, ConnectionId right)
		{
			return !left.Equals(right);
		}

		private readonly int _value;

		internal ConnectionId(int value)
		{
			_value = value;
		}
	}
}