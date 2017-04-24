namespace SharpRemote.WebApi.Requests
{
	/// <summary>
	/// Responsible for handling web-api requests.
	/// </summary>
	public interface IRequestHandler
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subUri"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		WebResponse TryHandle(string subUri, WebRequest request);
	}
}