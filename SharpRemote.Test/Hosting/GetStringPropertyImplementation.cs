using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Hosting
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