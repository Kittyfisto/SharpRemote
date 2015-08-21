using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Remoting.SocketRemotingEndPoint
{
	[TestFixture]
	public sealed class DisconnectTest
		: AbstractTest
	{
		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description(
			"Verifies that Disconnect() disconnects from the remote endpoint, sets the IsConnected property to false and the RemoteEndPoint property to null"
			)]
		public void TestDisconnect1()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);

				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				// Disconnecting from the endpoint that established the connection in the first place
				client.Disconnect();

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				// Unfortunately, for now, Disconnect() does not wait for approval of the remot endpoint and therefore we can't
				// immediately assert that rep2 is disconnected as well...
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description(
			"Verifies that Disconnect() disconnects from the remote endpoint, sets the IsConnected property to false and the RemoteEndPoint property to null"
			)]
		public void TestDisconnect2()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				// Disconnecting from the other endpoint
				server.Disconnect();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				// Unfortunately, for now, Disconnect() does not wait for approval of the remot endpoint and therefore we can't
				// immediately assert that rep1 is disconnected as well...
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that disconnecting and connecting to the same endpoint again is possible")]
		public void TestDisconnect3()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));
				client.IsConnected.Should().BeTrue();
				server.IsConnected.Should().BeTrue();

				client.Disconnect();
				client.IsConnected.Should().BeFalse();
				WaitFor(() => !server.IsConnected, TimeSpan.FromSeconds(2))
					.Should().BeTrue();

				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(1)))
					.ShouldNotThrow();
				client.IsConnected.Should().BeTrue();
				server.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Repeat(10)]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that once a socket is disconnected, all pending and future method calls are cancelled")]
		public void TestDisconnect4()
		{
			const int numTasks = 64;
			const int numMethodCalls = 1000;
			var timeout = TimeSpan.FromSeconds(5);

			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));

				var subject = new Mock<IVoidMethod>();
				server.CreateServant(1, subject.Object);
				var proxy = client.CreateProxy<IVoidMethod>(1);

				var tasks = Enumerable.Range(0, numTasks)
									  .Select(x => Task.Factory.StartNew(() =>
									  {
										  for (int n = 0; n < numMethodCalls; ++n)
										  {
											  proxy.DoStuff();

											  if (n == numMethodCalls / 2)
												  server.Disconnect();
										  }
									  }, TaskCreationOptions.LongRunning)).ToArray();

				foreach (var task in tasks)
				{
					bool thrown = false;
					try
					{
						task.Wait(timeout)
							.Should().BeTrue("Because the task certainly shouldn't have deadlocked");
					}
					catch (AggregateException e)
					{
						thrown = (e.InnerException is NotConnectedException ||
								  e.InnerException is ConnectionLostException);
						if (!thrown)
							throw;
					}

					thrown.Should().BeTrue("Because all tasks should've either thrown a connection lost or a not connected exception");
				}
			}
		}

		[Test]
		[Repeat(10)]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that once a socket is disconnected, all pending and future method calls are cancelled")]
		public void TestDisconnect5()
		{
			const int numTasks = 64;
			const int numMethodCalls = 1000;
			var timeout = TimeSpan.FromSeconds(5);

			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));

				var subject = new Mock<IReturnsTask>();
				subject.Setup(x => x.DoStuff()).Returns(() => Task.FromResult(1));

				server.CreateServant(1, subject.Object);
				var proxy = client.CreateProxy<IReturnsTask>(1);

				var tasks = Enumerable.Range(0, numTasks)
									  .Select(x => Task.Factory.StartNew(() =>
									  {
										  for (int n = 0; n < numMethodCalls; ++n)
										  {
											  proxy.DoStuff()
											       .Wait();

											  if (n == numMethodCalls / 2)
												  server.Disconnect();
										  }
									  }, TaskCreationOptions.LongRunning)).ToArray();

				foreach (var task in tasks)
				{
					bool thrown = false;
					try
					{
						task.Wait(timeout)
							.Should().BeTrue("Because the task certainly shouldn't have deadlocked");
					}
					catch (AggregateException e)
					{
						e = e.Flatten();
						e.InnerExceptions.Any(x => x is ConnectionLostException ||
						                           x is NotConnectedException).Should().BeTrue();
						thrown = true;
					}

					thrown.Should().BeTrue("Because all tasks should've either thrown a connection lost or a not connected exception");
				}
			}
		}
	}
}