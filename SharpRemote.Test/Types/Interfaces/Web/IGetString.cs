using SharpRemote.WebApi;

namespace SharpRemote.Test.Types.Interfaces.Web
{
	public interface IGetString
	{
		[HttpGet]
		string Get();

		[HttpGet("startIndex={0}&count={1}")]
		string Get(int startIndex, int count);
	}
}