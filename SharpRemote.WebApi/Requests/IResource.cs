namespace SharpRemote.WebApi.Requests
{
	/// <summary>
	///     Represents a resource exposed via a web api.
	///     Responsible for forwarding web requests to the actual class that controls
	///     access to the specific resource.
	/// </summary>
	internal interface IResource
	{
		/// <summary>
		/// Tries to fulfill the given request.
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		WebResponse TryHandleRequest(string uri, WebRequest request);
	}
}