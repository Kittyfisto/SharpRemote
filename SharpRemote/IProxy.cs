namespace SharpRemote
{
	/// <summary>
	/// Tag interface for a proxy implementation of a specific interface.
	/// Is responsible for delegating method invocations over a remoting channel
	/// to its actual implementation.
	/// </summary>
	public interface IProxy
		: IGrain
	{
	}
}