// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	internal class SerializationTypeChanged
		: ITypeModelDifference
	{
		private readonly string _assemblyQualifiedName;
		private readonly SerializationType _expectedType;
		private readonly SerializationType _actualType;

		public SerializationTypeChanged(string assemblyQualifiedName,
		                                SerializationType expectedType,
		                                SerializationType actualType)
		{
			_assemblyQualifiedName = assemblyQualifiedName;
			_expectedType = expectedType;
			_actualType = actualType;
		}

		#region Overrides of Object

		public override string ToString()
		{
			return string.Format("The type '{0}' is expected to be '{1}' but is actually '{2}'",
			                     _assemblyQualifiedName,
			                     _expectedType,
			                     _actualType);
		}

		#endregion
	}
}