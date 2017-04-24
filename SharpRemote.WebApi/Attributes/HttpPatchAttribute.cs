// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	public sealed class HttpPatchAttribute
		: HttpAttribute
	{
		/// <summary>
		/// 
		/// </summary>
		public HttpPatchAttribute()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="route"></param>
		public HttpPatchAttribute(string route)
		{
			Route = route;
		}

		/// <inheritdoc />
		public override HttpMethod Method => HttpMethod.Patch;
	}
}