using System.Net;
using WebRequest = SharpRemote.WebApi.Requests.WebRequest;
using WebResponse = SharpRemote.WebApi.Requests.WebResponse;

namespace SharpRemote.WebApi
{
	internal sealed class WebRequestContext
		: IWebRequestContext
	{
		private readonly HttpListenerContext _context;
		private readonly WebRequest _request;

		public WebRequestContext(HttpListenerContext context)
		{
			_context = context;
			_request = new WebRequest(context.Request);
		}

		public WebRequest Request => _request;

		public void SetResponse(WebResponse webResponse)
		{
			var response = _context.Response;
			response.StatusCode = webResponse.Code;
		}
	}
}