using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Interface for the serializer that must be capable of serializing arbitrary object graphs.
	/// </summary>
	public interface ISerializer
	{
		/// <summary>
		///     Registers the given type <typeparamref name="T" /> with this serializer.
		/// </summary>
		/// <remarks>
		///     This method can be used to verify upfront that:
		///     - A used type can be serialized
		///     - No intermittent compilation happens while writing / reading an object graph
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <exception cref="ArgumentNullException">When <typeparamref name="T" /> is null</exception>
		/// <exception cref="ArgumentException">When the type cannot be serialized</exception>
		/// <exception cref="SerializationException">
		///     In case there was an error while generating the code necessary for
		///     serialization / deserialization
		/// </exception>
		void RegisterType<T>();

		/// <summary>
		///     Registers the given type <paramref name="type" /> with this serializer.
		/// </summary>
		/// <remarks>
		///     This method can be used to verify upfront that:
		///     - A used type can be serialized
		///     - No intermittent compilation happens while writing / reading an object graph
		/// </remarks>
		/// <param name="type">The type to register</param>
		/// <exception cref="ArgumentNullException">When <paramref name="type" /> is null</exception>
		/// <exception cref="ArgumentException">When the type cannot be serialized</exception>
		/// <exception cref="SerializationException">
		///     In case there was an error while generating the code necessary for
		///     serialization / deserialization
		/// </exception>
		void RegisterType(Type type);

		/// <summary>
		///     Tests if the given type <typeparamref name="T" /> has already been registered
		///     with this serializer (either directly through <see cref="RegisterType" /> or indirectly
		///     through <see cref="WriteObject" /> or <see cref="ReadObject" />).
		/// </summary>
		/// <typeparam name="T">The type to test</typeparam>
		/// <returns>True if the type has been registered, false otherwise</returns>
		[Pure]
		bool IsTypeRegistered<T>();

		/// <summary>
		///     Tests if the given type <paramref name="type" /> has already been registered
		///     with this serializer (either directly through <see cref="RegisterType" /> or indirectly
		///     through <see cref="WriteObject" /> or <see cref="ReadObject" />).
		/// </summary>
		/// <param name="type">The type to test</param>
		/// <returns>True if the type has been registered, false otherwise</returns>
		[Pure]
		bool IsTypeRegistered(Type type);


		/// <summary>
		///     Writes the given object graph using the given <paramref name="writer" />.
		///     If the type is not registered yet, then it will be.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="endPoint"></param>
		/// <exception cref="SerializationException">
		///     In case there was an error while generating the code necessary for
		///     serialization / deserialization
		/// </exception>
		void WriteObject(BinaryWriter writer, object value, IRemotingEndPoint endPoint);

		/// <summary>
		///     Reads the next object graph using the given <paramref name="reader" />.
		///     If the type is not registered yet, then it will be.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		/// <exception cref="SerializationException">
		///     In case there was an error while generating the code necessary for
		///     serialization / deserialization
		/// </exception>
		object ReadObject(BinaryReader reader, IRemotingEndPoint endPoint);

		/// <summary>
		///     Resolves a type by its name.
		/// </summary>
		/// <param name="assemblyQualifiedTypeName"></param>
		/// <returns></returns>
		[Pure]
		Type GetType(string assemblyQualifiedTypeName);
	}
}