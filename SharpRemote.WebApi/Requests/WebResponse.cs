namespace SharpRemote.WebApi.Requests
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class WebResponse
	{
		private readonly int _code;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="code"></param>
		public WebResponse(int code)
		{
			_code = code;
		}

		/// <summary>
		/// 
		/// </summary>
		public int Code => _code;
	}
}