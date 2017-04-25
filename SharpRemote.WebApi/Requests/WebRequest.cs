using System;

namespace SharpRemote.WebApi.Requests
{
	/// <summary>
	/// </summary>
	public sealed class WebRequest
	{
		/// <summary>
		/// </summary>
		public Uri Url { get; set; }

		/// <summary>
		/// </summary>
		public HttpMethod Method { get; set; }
	}
}