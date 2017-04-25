using System.Net.Http;

namespace SharpRemote.Test.WebApi
{
	public static class HttpResponseMessageExtensions
	{
		public static string GetContent(this HttpResponseMessage message)
		{
			return message.Content.ReadAsStringAsync().Result;
		}
	}
}