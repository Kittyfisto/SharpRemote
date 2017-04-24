using System.Threading.Tasks;

namespace SharpRemote.WebApi.Requests
{
	/// <summary>
	/// Responsible for handling web-api requests and forwarding them to the actual controller.
	/// </summary>
	public interface IRequestHandler
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		WebResponse TryHandle(WebRequest request);
	}
}