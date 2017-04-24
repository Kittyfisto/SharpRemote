// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	public sealed class HttpDeleteAttribute
		: HttpAttribute
	{
		public HttpDeleteAttribute()
		{
		}

		public HttpDeleteAttribute(string route)
		{
		}

		/// <inheritdoc />
		public override HttpMethod Method => HttpMethod.Delete;
	}
}