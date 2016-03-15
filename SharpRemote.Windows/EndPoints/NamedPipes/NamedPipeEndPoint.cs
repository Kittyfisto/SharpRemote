using System.Net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class NamedPipeEndPoint
		: EndPoint
	{
		private readonly string _pipeName;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pipeName"></param>
		public NamedPipeEndPoint(string pipeName)
		{
			_pipeName = pipeName;
		}

		public string PipeName
		{
			get { return _pipeName; }
		}
	}
}