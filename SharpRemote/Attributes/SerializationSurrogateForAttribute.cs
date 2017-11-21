using System;
using System.Runtime.Serialization;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.Attributes
{
	/// <summary>
	///     This attributes identifies a type as being a surrogate type which shall
	///     be used *instead* of the real thing when serializing.
	/// </summary>
	/// <remarks>
	///     A surrogate requires two publicly accessible conversion methods which convert
	///     to and from their surrogate. <see cref="IPAddressSurrogate" /> for a specific
	///     example of how implement a non-generic surrogate and <see cref="KeyValuePairSurrogate{TKey, TValue}" />
	///     for a generic-surrogate.
	/// </remarks>
	/// <remarks>
	///     Surrogate types should be used when:
	///     A) You want to ensure that an object of type X can be serialized, but you have no control
	///     of its implementation and thus cannot add the appropriate <see cref="DataContractAttribute" />.
	///     B) You want to separate the serialization aspect from a type, for example because said
	///     type shall not expose public setters which are only necessary for serialization
	/// </remarks>
	/// <remarks>
	///     Surrogate types are not automatically identified and must be registered
	///     manually using <see cref="ISerializer2.RegisterType" />.
	///     If you create a generic type surrogate, then you can registers its open type definition once without
	///     having to call <see cref="ISerializer2.RegisterType" /> for every variation its of type arguments.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	internal sealed class SerializationSurrogateForAttribute
		: Attribute
	{
		private readonly Type _type;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="type"></param>
		public SerializationSurrogateForAttribute(Type type)
		{
			_type = type;
		}

		/// <summary>
		///     The type for which this type is a surrogate.
		/// </summary>
		public Type Type => _type;
	}
}