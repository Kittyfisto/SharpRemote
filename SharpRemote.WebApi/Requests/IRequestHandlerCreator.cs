namespace SharpRemote.WebApi.Requests
{
	internal interface IRequestHandlerCreator
	{
		IRequestHandler Create<T>(T controller);
	}
}