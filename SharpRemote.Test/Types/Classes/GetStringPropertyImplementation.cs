using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class GetStringPropertyImplementation
		: IGetStringProperty
	{
		public string Value
		{
			get { return "Foobar"; }
		}
	}
}