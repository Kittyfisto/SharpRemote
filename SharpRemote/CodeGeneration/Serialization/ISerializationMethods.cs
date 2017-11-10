using System;
using System.Reflection;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     An interface through which serialization and deserialization methods can be accessed
	///     for a particular <see cref="Type" />.
	/// </summary>
	public interface ISerializationMethods
	{
		/// <summary>
		///     A description of the type this object is responsible for serializing/deserializing.
		/// </summary>
		ITypeDescription TypeDescription { get; }

		/// <summary>
		///     The method through which a value (possibly-null) of the described type may be serialized.
		/// </summary>
		MethodInfo WriteValueMethod { get; }

		/// <summary>
		///     The method through which a value (non-null) of the described type may be serialized.
		/// </summary>
		MethodInfo WriteValueNotNullMethod { get; }

		/// <summary>
		///     The method through which a possibly null object of the described type may be serialized.
		/// </summary>
		MethodInfo WriteObjectMethod { get; }

		/// <summary>
		///     The method through which a value (possibly null) of the described type may be deserialized.
		/// </summary>
		MethodInfo ReadValueMethod { get; }

		/// <summary>
		///     The method through which a value (non-null) of the described type may be deserialized.
		/// </summary>
		MethodInfo ReadValueNotNullMethod { get; }

		/// <summary>
		///     The method through which possibly null object of the described type may be deserialized.
		/// </summary>
		MethodInfo ReadObjectMethod { get; }
	}
}