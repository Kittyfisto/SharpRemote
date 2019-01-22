// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	internal sealed class ParameterTypeMismatch
		: IncompatibleMethodSignature
	{
		private readonly ParameterDescription _expectedParameter;
		private readonly ParameterDescription _actualParameter;

		public ParameterTypeMismatch(ParameterDescription expectedParameter, ParameterDescription actualParameter)
		{
			_expectedParameter = expectedParameter;
			_actualParameter = actualParameter;
		}

		#region Overrides of Object

		public override string ToString()
		{
			return string.Format("Expected the parameter at index {0} to be of type '{1}', but found '{2}'",
			                     _expectedParameter.Position,
			                     _expectedParameter.ParameterType.AssemblyQualifiedName,
			                     _actualParameter.ParameterType.AssemblyQualifiedName);
		}

		#endregion
	}
}