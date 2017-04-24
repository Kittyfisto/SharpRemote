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
		public HttpPutAttribute()
		{ }

		public HttpPutAttribute(string route)
		{ }

		/// <inheritdoc />
		public override HttpMethod Method { get; } = HttpMethod.Put;
	}
}