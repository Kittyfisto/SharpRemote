namespace SharpRemote.WebApi.Requests
{
	internal sealed class RequestHandlerCreator
		: IRequestHandlerCreator
	{
		public IRequestHandler Create<T>(T controller)
		{
			return new RequestHandler(typeof(T), controller);
		}
	}
}