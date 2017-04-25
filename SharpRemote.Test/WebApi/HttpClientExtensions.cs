using System;
using System.Net.Http;

namespace SharpRemote.Test.WebApi
{
	public static class HttpClientExtensions
	{
		public static HttpResponseMessage Get(this HttpClient client, Uri uri)
		{
			var task = client.GetAsync(uri);
			task.Wait();
			return task.Result;
		}
	}
}