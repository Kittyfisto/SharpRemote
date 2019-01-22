// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///    An interface is missing a method.
	/// </summary>
	internal sealed class MissingMethod
		: ITypeModelDifference
	{
		private readonly TypeDescription _typeDescription;
		private readonly MethodDescription _expectedMethod;

		public MissingMethod(TypeDescription typeDescription, MethodDescription expectedMethod)
		{
			_typeDescription = typeDescription;
			_expectedMethod = expectedMethod;
		}

		#region Overrides of Object

		public override string ToString()
		{
			return string.Format("The type '{0}' is missing a method named '{1}' - has it been renamed?",
			                     _typeDescription.AssemblyQualifiedName,
			                     _expectedMethod.Name);
		}

		#endregion
	}
}