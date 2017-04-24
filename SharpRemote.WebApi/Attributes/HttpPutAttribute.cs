// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class HttpPutAttribute
		: HttpAttribute
	{
		/// <summary>
		/// 
		/// </summary>
		public HttpPutAttribute()
		{ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="route"></param>
		public HttpPutAttribute(string route)
		{
			Route = route;
		}

		/// <inheritdoc />
		public override HttpMethod Method { get; } = HttpMethod.Put;
	}
}