// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///    An expected type is completely missing (maybe it has been renamed).
	/// </summary>
	internal sealed class MissingValueType
		: ITypeModelDifference
	{
		private readonly string _typeName;

		public MissingValueType(string typeName)
		{
			_typeName = typeName;
		}

		#region Overrides of Object

		public override string ToString()
		{
			return
				string.Format("The type '{0}' is missing. If it has been renamed, then consider applying the [DataContract(Name=\"...\")] attribute while specifying its old name",
				              _typeName);
		}

		#endregion
	}
}