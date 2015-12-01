using System.Diagnostics;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class ReturnsPid
		: IGetInt32Property
	{
		public int Value
		{
			get
			{
				var process = Process.GetCurrentProcess();
				var id = process.Id;
				return id;
			}
		}
	}
}
