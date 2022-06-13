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

#if NET6_0
		private static readonly TypeLoader TypeLoader = new TypeLoader();
#endif

		static TypeResolver()
		{
			Cache = new Dictionary<string, Type>();
		}

		/// <summary>
		/// Resolves the type for the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="throwOnError"></param>
		/// <returns></returns>
		[Pure]
		public static Type GetType(string name, bool throwOnError = false)
		{
			lock (Cache)
			{
				if (!Cache.TryGetValue(name, out var value))
				{
					value = LoadType(name, throwOnError);
					if (value == null)
						return null;

					Cache.Add(name, value);
				}

				return value;
			}
		}

		private static Type LoadType(string name, bool throwOnError)
		{
#if NET6_0
			var typeName = TypeName.Parse(name);
			// in .NET 6 the assembly is not loaded automatically
			return TypeLoader.LoadType(typeName.FullTypeNameWithNamespace, typeName.AssemblyName, throwOnError);
#else
			return Type.GetType(name, throwOnError);
#endif
		}
	}
}