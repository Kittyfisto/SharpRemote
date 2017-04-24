using System;
using System.Net;

namespace SharpRemote.WebApi.Requests
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class WebRequest
	{
		/// <summary>
		/// 
		/// </summary>
		public Uri Url { get; }

		/// <summary>
		/// 
		/// </summary>
		public HttpMethod Method { get; }

		internal WebRequest(HttpListenerRequest request)
		{
			Url = request.Url;
			Method = (HttpMethod) Enum.Parse(typeof(HttpMethod), request.HttpMethod, true);
		}
	}
}