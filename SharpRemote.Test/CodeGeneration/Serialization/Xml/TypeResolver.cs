using System;

namespace SharpRemote.Test.CodeGeneration.Serialization.Xml
{
	internal sealed class TypeResolver
		: ITypeResolver
	{
		public Type GetType(string assemblyQualifiedTypeName)
		{
			return Type.GetType(assemblyQualifiedTypeName);
		}
	}
}