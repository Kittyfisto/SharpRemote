// ReSharper disable CheckNamespace

namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	public sealed class HttpGetAttribute
		: HttpAttribute
	{
		public HttpGetAttribute()
		{
		}

		public HttpGetAttribute(string route)
		{
		}

		/// <inheritdoc />
		public override HttpMethod Method => HttpMethod.Get;
	}
}