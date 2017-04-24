// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	public sealed class HttpDeleteAttribute
		: HttpAttribute
	{
		/// <summary>
		/// 
		/// </summary>
		public HttpDeleteAttribute()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="route"></param>
		public HttpDeleteAttribute(string route)
		{
			Route = route;
		}

		/// <inheritdoc />
		public override HttpMethod Method => HttpMethod.Delete;
	}
}