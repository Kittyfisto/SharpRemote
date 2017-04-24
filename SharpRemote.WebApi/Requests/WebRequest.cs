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

		internal WebRequest(HttpListenerRequest request)
		{
			Url = request.Url;
		}
	}
}