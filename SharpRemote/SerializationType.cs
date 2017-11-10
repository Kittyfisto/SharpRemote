using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     Describes how a given type is treated by the <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public enum SerializationType
	{
		/// <summary>
		///     The type cannot be serialized.
		/// </summary>
		[EnumMember] NotSerializable = 0,

		/// <summary>
		///     The type is assumed to be an acyclic graph where each node is itself one
		///     of the three types.
		/// </summary>
		[EnumMember] ByValue = 1,

		/// <summary>
		///     The type is assumed to be a reference type and thus no serialization is performed:
		///     Instead whenever a method/property of the type is accessed, another RPC is performed.
		/// </summary>
		[EnumMember] ByReference = 2,

		/// <summary>
		///     The type is assumed to be a singleton and the instance can be retrieved via
		///     the method with the <see cref="SingletonFactoryMethodAttribute" />.
		/// </summary>
		[EnumMember] Singleton = 3
	}
}