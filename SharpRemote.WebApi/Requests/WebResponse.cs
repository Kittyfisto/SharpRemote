using System.Text;

namespace SharpRemote.WebApi.Requests
{
	/// <summary>
	/// </summary>
	public sealed class WebResponse
	{
		/// <summary>
		/// </summary>
		/// <param name="code"></param>
		public WebResponse(int code)
		{
			Code = code;
		}

		/// <summary>
		/// </summary>
		/// <param name="code"></param>
		/// <param name="content"></param>
		public WebResponse(int code, string content)
		{
			Code = code;
			Encoding = Encoding.UTF8;
			Content = Encoding.GetBytes(content);
		}

		/// <summary>
		/// 
		/// </summary>
		public byte[] Content { get; }

		/// <summary>
		/// 
		/// </summary>
		public Encoding Encoding { get; }

		/// <summary>
		/// </summary>
		public int Code { get; }
	}
}