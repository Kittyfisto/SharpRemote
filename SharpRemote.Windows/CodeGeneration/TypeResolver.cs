using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SharpRemote.CodeGeneration
{
	/// <summary>
	///     Responsible for resolving <see cref="Type" /> objects by their names.
	/// </summary>
	public static class TypeResolver
	{
		private static readonly Dictionary<string, Type> Cache;

		static TypeResolver()
		{
			Cache = new Dictionary<string, Type>();
		}

		/// <summary>
		/// Resolves the type for the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[Pure]
		public static Type GetType(string name)
		{
			lock (Cache)
			{
				Type value;
				if (!Cache.TryGetValue(name, out value))
				{
					value = Type.GetType(name);
					Cache.Add(name, value);
				}
				return value;
			}
		}

		/// <summary>
		/// Resolves the type for the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="throwOnError"></param>
		/// <returns>The given type with the given name or null in case no such type could be found</returns>
		[Pure]
		public static Type GetType(string name, bool throwOnError)
		{
			lock (Cache)
			{
				Type value;
				if (!Cache.TryGetValue(name, out value))
				{
					value = Type.GetType(name, throwOnError);
					if (value == null)
						return null;

					Cache.Add(name, value);
				}
				return value;
			}
		}
	}
}