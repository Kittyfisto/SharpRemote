using System.Threading.Tasks;

namespace SharpRemote.Test.Types.Interfaces
{
	public interface IReturnsIntTaskMethodString
	{
		Task<int> CreateFile(string fileName);
	}
}