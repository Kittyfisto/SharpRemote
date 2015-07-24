using System.Threading.Tasks;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class ReturnsTask
		: IReturnsTask
	{
		public Task DoStuff()
		{
			return Task.FromResult(int.MaxValue);
		}
	}
}