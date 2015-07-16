using System.Threading.Tasks;

namespace SharpRemote.Hosting
{
	internal sealed class Heartbeat
		: IHeartbeat
	{
		public Task Beat()
		{
			return Task.FromResult(1);
		}
	}
}