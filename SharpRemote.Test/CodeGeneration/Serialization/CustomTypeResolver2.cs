using System;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	internal sealed class CustomTypeResolver2
		: ITypeResolver
	{
		private readonly Func<string, Type> _fn;

		public CustomTypeResolver2(Func<string, Type> fn)
		{
			_fn = fn;
		}

		public Type GetType(string assemblyQualifiedTypeName)
		{
			return _fn(assemblyQualifiedTypeName);
		}
	}
}