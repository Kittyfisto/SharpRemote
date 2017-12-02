using System;
using System.Reflection;
using log4net;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Responsible for ensuring that user-supplied type resolvers all behave identical
	///     when they fail to resolve a type.
	/// </summary>
	internal sealed class TypeResolverAdapter
		: ITypeResolver
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		private readonly ITypeResolver _typeResolver;

		public TypeResolverAdapter(ITypeResolver typeResolver)
		{
			_typeResolver = typeResolver;
		}

		public Type GetType(string assemblyQualifiedTypeName)
		{
			try
			{
				var type = _typeResolver != null
					? _typeResolver.GetType(assemblyQualifiedTypeName)
					: TypeResolver.GetType(assemblyQualifiedTypeName);

				if (type == null)
				{
					var errorMessage = string.Format("Unable to load '{0}': The type resolver returned null", assemblyQualifiedTypeName);
					Log.Error(errorMessage);
					throw new TypeLoadException(errorMessage);
				}

				if (Log.IsDebugEnabled)
				{
					Log.DebugFormat("Resolved '{0}' to '{1}'", assemblyQualifiedTypeName, type);
				}

				return type;
			}
			catch (TypeLoadException e)
			{
				Log.ErrorFormat("Caught exception while resolving '{0}': {1}", assemblyQualifiedTypeName, e);
				throw;
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Unable to load '{0}', the type resolver threw an exception while resolving the type: {1}",
				                assemblyQualifiedTypeName, e);
				throw new
					TypeLoadException(string.Format("Unable to load '{0}': The type resolver threw an exception while resolving the type",
					                                assemblyQualifiedTypeName), e);
			}
		}
	}
}