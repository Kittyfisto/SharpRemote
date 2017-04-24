// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	public sealed class HttpPostAttribute
		: HttpAttribute
	{
		/// <summary>
		/// 
		/// </summary>
		public HttpPostAttribute()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="route"></param>
		public HttpPostAttribute(string route)
		{
			Route = route;
		}

		/// <inheritdoc />
		public override HttpMethod Method => HttpMethod.Post;
	}
}