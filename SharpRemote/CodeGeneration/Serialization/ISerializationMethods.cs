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
		///     The method through which a value of the described type may be serialized.
		/// </summary>
		MethodInfo WriteValueMethod { get; }

		/// <summary>
		///     The method through which an object of the described type may be serialized.
		/// </summary>
		MethodInfo WriteObjectMethod { get; }

		/// <summary>
		///     The method through which a value of the described type may be deserialized.
		/// </summary>
		MethodInfo ReadValueMethod { get; }

		/// <summary>
		///     The method through which an object of the described type may be deserialized.
		/// </summary>
		MethodInfo ReadObjectMethod { get; }
	}
}