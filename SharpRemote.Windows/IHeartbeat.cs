using System.Threading.Tasks;

namespace SharpRemote
{
	/// <summary>
	/// This interface is used to detected failures of remote endpoints.
	/// </summary>
	public interface IHeartbeat
	{
		/// <summary>
		/// Called regularly in order to detect whether or not the remote endpoint is still alive or not.
		/// </summary>
		Task Beat();
	}
}