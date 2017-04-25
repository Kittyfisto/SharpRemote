using SharpRemote.WebApi;

namespace SharpRemote.Test.Types.Interfaces.Web
{
	public interface ITwoIdenticalRoutes
	{
		[Route]
		string GetBar();

		[Route]
		string GetFoo();
	}
}