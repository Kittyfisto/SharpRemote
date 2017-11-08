namespace SharpRemote
{
	/// <summary>
	/// Tag interface for a servant implementation of a specific interface.
	/// Is responsible for invoking remote method invocations from a remoting channel
	/// to its actual implementation.
	/// </summary>
	public interface IServant
		: IGrain
	{
		/// <summary>
		/// The subject who's methods are being invoked.
		/// </summary>
		object Subject { get; }
	}
}