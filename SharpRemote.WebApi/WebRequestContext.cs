using System;
using System.IO;
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
			_request = CreateRequest(context.Request);
		}

		private WebRequest CreateRequest(HttpListenerRequest request)
		{
			return new WebRequest
			{
				Url = request.Url,
				Method = (HttpMethod) Enum.Parse(typeof(HttpMethod), request.HttpMethod, true)
			};
		}

		public WebRequest Request => _request;

		public void SetResponse(WebResponse webResponse)
		{
			var response = _context.Response;
			response.StatusCode = webResponse.Code;
			response.ContentEncoding = webResponse.Encoding;
			using (var writer = new BinaryWriter(response.OutputStream))
			{
				writer.Write(webResponse.Content);
			}
			response.Close();
		}
	}
}