// ReSharper disable CheckNamespace

namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	public sealed class HttpGetAttribute
		: HttpAttribute
	{
		/// <summary>
		/// 
		/// </summary>
		public HttpGetAttribute()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="route"></param>
		public HttpGetAttribute(string route)
		{
			Route = route;
		}

		/// <inheritdoc />
		public override HttpMethod Method => HttpMethod.Get;
	}
}