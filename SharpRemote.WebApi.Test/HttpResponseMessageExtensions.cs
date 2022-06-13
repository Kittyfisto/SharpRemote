using System.Net.Http;

namespace SharpRemote.WebApi.Test
{
	public static class HttpResponseMessageExtensions
	{
		public static string GetContent(this HttpResponseMessage message)
		{
			return message.Content.ReadAsStringAsync().Result;
		}
	}
}