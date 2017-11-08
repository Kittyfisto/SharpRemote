using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     A globally (seriously) unique identifier for a <see cref="ITypeDescription" /> and its .NET type.
	/// </summary>
	[DataContract]
	public struct TypeId : IEquatable<TypeId>
	{
		/// <inheritdoc />
		public bool Equals(TypeId other)
		{
			return _value.Equals(other._value);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(objA: null, objB: obj)) return false;
			return obj is TypeId && Equals((TypeId) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

		/// <summary>
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(TypeId left, TypeId right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(TypeId left, TypeId right)
		{
			return !left.Equals(right);
		}

		private Guid _value;

		/// <summary>
		/// </summary>
		[DataMember]
		[Obsolete("To be used by the serializer only", error: true)]
		public Guid SerializedValue
		{
			get { return _value; }
			set { _value = value; }
		}

		/// <summary>
		///     The actual value of this id.
		/// </summary>
		public Guid Value => _value;
	}
}