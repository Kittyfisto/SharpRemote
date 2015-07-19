using System;

namespace SharpRemote.Hosting
{
	public interface ILatency
	{
		void Roundtrip();
	}

	public sealed class Latency
		: ILatency
	{
		public void Roundtrip()
		{
			
		}
	}
}