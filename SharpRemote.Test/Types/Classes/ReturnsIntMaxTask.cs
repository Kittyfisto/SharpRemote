using System.Threading.Tasks;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class ReturnsIntMaxTask
		: IReturnsIntTask
	{
		public Task<int> DoStuff()
		{
			return Task.FromResult(int.MaxValue);
		}
	}
}
