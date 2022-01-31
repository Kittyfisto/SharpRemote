namespace SharpRemote.WebApi.Test
{
	public interface ITwoIdenticalRoutes
	{
		[Route]
		string GetBar();

		[Route]
		string GetFoo();
	}
}