using System;
using System.Net;
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
		[EnumMember] NoneSerializable = 0,

		/// <summary>
		///     One of the "plain old data" types, i.e. types where <see cref="Type.IsPrimitive" /> is true.
		/// </summary>
		[EnumMember] Pod = 1,

		/// <summary>
		///     One of the built-in types where a custom serializer exists already (types such as <see cref="IPAddress" />,
		///     <see cref="DateTime" />, etc...).
		/// </summary>
		[EnumMember] BuiltIn = 2,

		/// <summary>
		///     The type is assumed to be an acyclic graph where each node is itself one
		///     of the three types.
		/// </summary>
		[EnumMember] DataContract = 3,

		/// <summary>
		///     The type is assumed to be a singleton and the instance can be retrieved via
		///     the method with the <see cref="SingletonFactoryMethodAttribute" />.
		/// </summary>
		[EnumMember] Singleton = 4,

		/// <summary>
		///     The type is assumed to be a reference type and thus no serialization is performed:
		///     Instead whenever a method/property of the type is accessed, another RPC is performed.
		/// </summary>
		[EnumMember] ByReference = 5
	}
}