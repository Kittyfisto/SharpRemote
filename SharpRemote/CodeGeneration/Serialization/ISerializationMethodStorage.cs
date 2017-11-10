using System;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Provides access to serialization methods on a per <see cref="Type"/> basis.
	/// </summary>
	public interface ISerializationMethodStorage<out T>
		where T : ISerializationMethods
	{
		/// <summary>
		///     Returns the serialization methods to serialize/deserialize the given .NET <paramref name="type" />.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		T GetOrAdd(Type type);
	}
}