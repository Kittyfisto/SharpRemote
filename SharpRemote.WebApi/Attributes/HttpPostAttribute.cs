// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	public sealed class HttpPostAttribute
		: HttpAttribute
	{
		public HttpPostAttribute()
		{
		}

		public HttpPostAttribute(string route)
		{
		}

		/// <inheritdoc />
		public override HttpMethod Method => HttpMethod.Post;
	}
}