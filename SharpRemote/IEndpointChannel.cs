using System.IO;

namespace SharpRemote
{
	public interface IEndPointChannel
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectId"></param>
		/// <param name="methodName"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		Stream CallRemoteMethod(ulong objectId, string methodName, Stream arguments);
	}
}