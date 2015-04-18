using System.IO;

namespace SharpRemote
{
	public sealed class EndPointChannel
		: IEndPointChannel
	{
		public Stream CallRemoteMethod(ulong objectId, string methodName, Stream arguments)
		{
			throw new NotConnectedException();
		}
	}
}