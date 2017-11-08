using System.Net;

// ReSharper disable CheckNamespace

namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	internal sealed class NamedPipeEndPoint
		: EndPoint
	{
		/// <summary>
		/// </summary>
		public enum PipeType
		{
			/// <summary>
			///     This endpoint represents a server pipe.
			/// </summary>
			Server,

			/// <summary>
			///     This endpoint represents a client pipe.
			/// </summary>
			Client,

			None,
		}

		private readonly string _pipeName;
		private readonly PipeType _type;

		/// <summary>
		/// </summary>
		/// <param name="pipeName"></param>
		/// <param name="type"></param>
		public NamedPipeEndPoint(string pipeName, PipeType type)
		{
			_pipeName = pipeName;
			_type = type;
		}

		/// <summary>
		/// </summary>
		public PipeType Type
		{
			get { return _type; }
		}

		/// <summary>
		/// </summary>
		public string PipeName
		{
			get { return _pipeName; }
		}

		public bool Equals(NamedPipeEndPoint other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(_pipeName, other._pipeName) && _type == other._type;
		}

		/// <summary>
		///     Creates a new <see cref="NamedPipeEndPoint" /> for a server-side named pipe.
		/// </summary>
		/// <param name="pipeName"></param>
		/// <returns></returns>
		public static NamedPipeEndPoint FromServer(string pipeName)
		{
			return new NamedPipeEndPoint(pipeName, PipeType.Server);
		}

		/// <summary>
		///     Creates a new <see cref="NamedPipeEndPoint" /> for a client-side named pipe.
		/// </summary>
		/// <param name="pipeName"></param>
		/// <returns></returns>
		public static NamedPipeEndPoint FromClient(string pipeName)
		{
			return new NamedPipeEndPoint(pipeName, PipeType.Client);
		}

		public override string ToString()
		{
			return string.Format("{0} ({1})", _pipeName, _type);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is NamedPipeEndPoint && Equals((NamedPipeEndPoint) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((_pipeName != null ? _pipeName.GetHashCode() : 0)*397) ^ (int) _type;
			}
		}

		/// <summary>
		/// Compares two <see cref="NamedPipeEndPoint"/>s for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(NamedPipeEndPoint left, NamedPipeEndPoint right)
		{
			return Equals(left, right);
		}

		/// <summary>
		/// Compares two <see cref="NamedPipeEndPoint"/>s for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(NamedPipeEndPoint left, NamedPipeEndPoint right)
		{
			return !Equals(left, right);
		}
	}
}