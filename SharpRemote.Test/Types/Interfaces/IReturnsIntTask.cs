using System.Threading.Tasks;

namespace SharpRemote.Test.Types.Interfaces
{
	public interface IReturnsIntTask
	{
		Task<int> DoStuff();
	}
}