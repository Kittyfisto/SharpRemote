// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class EndPointSettings
	{
		/// <summary>
		/// The maximum number of pending, concurrent calls at any given time.
		/// Any further asynchronous call will block until the call is finished.
		/// </summary>
		/// <remarks>
		/// Defaults to 2000.
		/// </remarks>
		public int MaxConcurrentCalls = 2000;
	}
}