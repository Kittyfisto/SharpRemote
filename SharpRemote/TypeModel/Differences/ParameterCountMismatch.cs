// ReSharper disable once CheckNamespace

namespace SharpRemote
{
	/// <summary>
	///     Indicates that a method has a different amount of parameters than expected.
	/// </summary>
	internal sealed class ParameterCountMismatch
		: IncompatibleMethodSignature
	{
		private readonly MethodDescription _actualMethod;
		private readonly MethodDescription _expectedMethod;

		public ParameterCountMismatch(MethodDescription expectedMethod, MethodDescription actualMethod)
		{
			_expectedMethod = expectedMethod;
			_actualMethod = actualMethod;
		}

		#region Overrides of Object

		public override string ToString()
		{
			return string.Format("The method '{0}' is expected to have {1} parameters but it actually has {2}",
			                     _expectedMethod.Name,
			                     _expectedMethod.Parameters.Length,
			                     _actualMethod.Parameters.Length);
		}

		#endregion
	}
}