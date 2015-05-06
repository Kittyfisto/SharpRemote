using System;
using System.IO;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.CodeGeneration
{
	public class Test
		: IEventInt32
	{
		private IEventInt32 _blubba;
		private IEndPointChannel _channel;
		private readonly ulong _objectId;

		public Test()
		{
			_blubba.Foobar += Invoke2;
		}

		public void OnFoobar(int value)
		{
			var stream = new MemoryStream();
			var writer = new BinaryWriter(stream);
			writer.Write(value);
			writer.Flush();
			stream.Position = 0;
			_channel.CallRemoteMethod(_objectId, "OnFoobar", stream);
		}

		public void Invoke2(int n)
		{
			var fn = Foobar;
			if (fn != null)
				fn(n);
		}

		public event Action<int> Foobar;
	}
}