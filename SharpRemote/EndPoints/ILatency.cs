// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// The interface that must be installed on both end-points in order to measure the average latency
	/// of RPC invocations.
	/// </summary>
	public interface ILatency
	{
		/// <summary>
		/// This method is called regularly and shouldn't do any work, ideally.
		/// </summary>
		void Roundtrip();
	}
}