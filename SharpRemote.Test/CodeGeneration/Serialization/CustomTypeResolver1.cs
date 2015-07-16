using System;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	internal sealed class CustomTypeResolver1
		: ITypeResolver
	{
		public Type GetType(string assemblyQualifiedTypeName)
		{
			++GetTypeCalled;
			return Type.GetType(assemblyQualifiedTypeName);
		}

		public int GetTypeCalled;
	}
}