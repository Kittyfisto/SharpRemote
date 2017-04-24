using SharpRemote.WebApi;

namespace SharpRemote.Test.Types.Interfaces.Web
{
	public interface IGetString
	{
		[HttpGet]
		string Get();
	}
}