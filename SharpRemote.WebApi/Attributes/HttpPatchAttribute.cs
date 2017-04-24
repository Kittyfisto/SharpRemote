// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// </summary>
	public sealed class HttpPatchAttribute
		: HttpAttribute
	{
		public HttpPatchAttribute()
		{
		}

		public HttpPatchAttribute(string route)
		{
		}

		/// <inheritdoc />
		public override HttpMethod Method => HttpMethod.Patch;
	}
}