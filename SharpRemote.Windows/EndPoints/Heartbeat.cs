using System.Threading.Tasks;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// Default <see cref="IHeartbeat"/> implementation that returns immediately.
	/// </summary>
	internal sealed class Heartbeat
		: IHeartbeat
	{
		public Task Beat()
		{
			return Task.FromResult(1);
		}
	}
}