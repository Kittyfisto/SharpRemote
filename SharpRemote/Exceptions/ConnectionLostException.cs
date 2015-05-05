// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a synchronous method on the proxy, or an event on the servant, is called and the connection
	/// has been interrupted while the call was ongoing.
	/// </summary>
	public class ConnectionLostException
		: RemotingException
	{
		
	}
}