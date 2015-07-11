using System;

namespace SharpRemote.CodeGeneration
{
	/// <summary>
	///     Responsible for resolving <see cref="Type" /> objects by their names.
	/// </summary>
	public static class TypeResolver
	{
		/// <summary>
		/// Resolves the type for the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Type GetType(string name)
		{
			return Type.GetType(name);
		}
	}
}