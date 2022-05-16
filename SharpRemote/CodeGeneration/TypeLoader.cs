#if NET6_0
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace SharpRemote.CodeGeneration
{
	public sealed class TypeLoader
	{
		private readonly object _resolutionLock = new object();

		public Type LoadType(string typeName, AssemblyName assemblyName, bool throwOnError)
		{
			var context = AssemblyLoadContext.Default;
			lock (_resolutionLock)
			{
				var type = (from loadedAssembly in AppDomain.CurrentDomain.GetAssemblies()
					let foundType = loadedAssembly.GetType(typeName, false)
					where foundType != null
					select foundType).FirstOrDefault();

				if (type != null)
					return type;

				context.Resolving += OnContextResolving;
				var assembly = context.LoadFromAssemblyName(assemblyName);
				context.Resolving -= OnContextResolving;

				type = assembly.GetType(typeName, throwOnError);
				return type;
			}
		}

		private static Assembly OnContextResolving(AssemblyLoadContext context, AssemblyName assemblyName)
		{
			var expectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName.Name + ".dll");
			return context.LoadFromAssemblyPath(expectedPath);
		}
	}
}
#endif