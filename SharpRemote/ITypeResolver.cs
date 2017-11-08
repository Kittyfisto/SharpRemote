using System;
using System.Diagnostics.Contracts;

namespace SharpRemote
{
	/// <summary>
	/// The interface to resolve types by their assembly qualified names.
	/// Can be implemented and given to the serializer, when <see cref="Type.GetType(string)"/> is
	/// insufficient.
	/// </summary>
	public interface ITypeResolver
	{
		/// <summary>
		/// Is called whenever a type object needs to be serialized.
		/// </summary>
		/// <param name="assemblyQualifiedTypeName"></param>
		/// <returns></returns>
		[Pure]
		Type GetType(string assemblyQualifiedTypeName);
	}
}