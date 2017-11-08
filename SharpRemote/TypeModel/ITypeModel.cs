using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     A representation of all types registered with a <see cref="ISerializer" />.
	///     This representation describes each type (as far as serialization is concerned) and
	///     may be serialized/deserialized *without* requiring that the types describes by this type model
	///     can be loaded.
	/// </summary>
	public interface ITypeModel
	{
		/// <summary>
		///     The types part of this model.
		/// </summary>
		IReadOnlyList<TypeDescription> Types { get; }
	}
}