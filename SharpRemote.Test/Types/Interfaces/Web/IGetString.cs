using SharpRemote.WebApi;

namespace SharpRemote.Test.Types.Interfaces.Web
{
	public interface IGetString
	{
		[Route]
		string Get();

		[Route("startIndex={0}&count={1}")]
		string Get(int startIndex, int count);
	}
}