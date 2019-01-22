// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	internal sealed class MissingType : ITypeModelDifference
	{
		private readonly TypeDescription _type;

		public MissingType(TypeDescription type)
		{
			_type = type;
		}

		#region Overrides of Object

		public override string ToString()
		{
			return string.Format("The type '{0}' is missing - has it been renamed?", _type.AssemblyQualifiedName);
		}

		#endregion
	}
}