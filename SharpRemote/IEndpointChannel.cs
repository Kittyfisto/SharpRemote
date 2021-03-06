using System.IO;
using System.Threading.Tasks;

namespace SharpRemote
{
	/// <summary>
	/// The interface to forward a method call to a remote endpoint and return its response.
	/// </summary>
	public interface IEndPointChannel
	{
		/// <summary>
		/// Forwards a method call to the given servant or proxy.
		/// </summary>
		/// <remarks>
		/// The returned task throws if
		/// - no servant / proxy with the given id exists
		/// - the servant / proxy doesn't implement the given interface
		/// - the method doesn't exist
		/// - The arguments are malformatted
		/// </remarks>
		/// <param name="servantId"></param>
		/// <param name="interfaceType"></param>
		/// <param name="methodName"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		Task<MemoryStream> CallRemoteMethodAsync(ulong servantId, string interfaceType, string methodName, MemoryStream arguments);

		/// <summary>
		/// Forwards a method call to the given servant or proxy.
		/// </summary>
		/// <remarks>
		/// Throws if
		/// - no servant / proxy with the given id exists
		/// - the servant / proxy doesn't implement the given interface
		/// - the method doesn't exist
		/// - The arguments are malformatted
		/// </remarks>
		/// <param name="servantId"></param>
		/// <param name="interfaceType"></param>
		/// <param name="methodName"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		MemoryStream CallRemoteMethod(ulong servantId, string interfaceType, string methodName, MemoryStream arguments);
	}
}