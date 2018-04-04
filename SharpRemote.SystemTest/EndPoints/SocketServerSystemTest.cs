using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.SystemTest.EndPoints
{
	[TestFixture]
	public sealed class SocketServerSystemTest
	{
		private Random _random;

		public interface IServer
		{
			Task GetDataAsync(IListener listener);
		}

		/// <summary>
		///     Sends a blob in small chunks to a listener.
		/// </summary>
		sealed class Server
			: IServer
		{
			private readonly byte[] _dataToSend;

			public Server(byte[] dataToSend)
			{
				_dataToSend = dataToSend;
			}

			public Task GetDataAsync(IListener listener)
			{
				const int chunkSize = 1000;
				int chunkCount = (int)Math.Ceiling(1.0* _dataToSend.Length / chunkSize);

				Task lastTask = null;
				for (int i = 0; i < chunkCount; ++i)
				{
					var chunk = GetChunk(i * chunkSize, chunkSize);
					lastTask = listener.SendChunkAsync(chunk);
				}

				// SharpRemote promises that calls on SendChunkAsync are serialized
				// and therefore we only have to wait for the last call to succeed
				return lastTask;
			}

			private byte[] GetChunk(int offset, int maxLength)
			{
				var remaining = _dataToSend.Length - offset;
				var length = Math.Min(remaining, maxLength);
				var chunk = new byte[length];
				Array.Copy(_dataToSend, offset, chunk, 0, length);
				return chunk;
			}
		}

		[ByReference]
		public interface IListener
		{
			[Invoke(Dispatch.SerializePerObject)]
			Task SendChunkAsync(byte[] chunk);
		}

		sealed class Listener
			: IListener
		{
			private readonly MemoryStream _buffer;

			public Listener()
			{
				_buffer = new MemoryStream();
			}

			public byte[] GetData()
			{
				return _buffer.ToArray();
			}

			public Task SendChunkAsync(byte[] chunk)
			{
				_buffer.Write(chunk, 0, chunk.Length);
				return Task.FromResult(42);
			}
		}

		[SetUp]
		public void Setup()
		{
			_random = new Random(42);
		}

		[Test]
		[Description("Verifies that two clients can concurrently receive data from one server")]
		public void TestSendTenMegabytes()
		{
			var testData = GenerateData(10 * 1024 * 1024);

			using (var server = CreateServer())
			using (var client1 = CreateClient())
			using (var client2 = CreateClient())
			{
				const int objectId = 42;

				var actualServer = new Server(testData);
				server.RegisterSubject<IServer>(objectId, actualServer);
				var client1ServerProxy = client1.CreateProxy<IServer>(objectId);
				var client2ServerProxy = client2.CreateProxy<IServer>(objectId);

				server.Bind(IPAddress.Loopback);
				client1.Connect(server.LocalEndPoint);
				client2.Connect(server.LocalEndPoint);

				var client1Listener = new Listener();
				var client2Listener = new Listener();

				var tasks = new[]
				{
					client1ServerProxy.GetDataAsync(client1Listener),
					client2ServerProxy.GetDataAsync(client2Listener)
				};
				Task.WaitAll(tasks);

				const string reason = "because the entire test data blob should've been received";
				client1Listener.GetData().Should().Equal(testData, reason);
				client2Listener.GetData().Should().Equal(testData, reason);
			}
		}

		private byte[] GenerateData(int length)
		{
			var buffer = new byte[length];
			_random.NextBytes(buffer);
			return buffer;
		}

		private SocketServer CreateServer()
		{
			return new SocketServer();
		}

		private SocketEndPoint CreateClient()
		{
			return new SocketEndPoint(EndPointType.Client);
		}
	}
}
