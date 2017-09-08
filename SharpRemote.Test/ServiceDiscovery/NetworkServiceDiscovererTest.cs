using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.ServiceDiscovery
{
	[TestFixture]
	public sealed class NetworkServiceDiscovererTest
	{
		private NetworkServiceDiscoverer _discoverer;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			_discoverer = new NetworkServiceDiscoverer();
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			_discoverer.Dispose();
		}

		[Test]
		[SetCulture("en-US")]
		[Description("Verifies that if a service is registered with a payload which is too big, then an exception is thrown")]
		public void TestRegisterTooBig()
		{
			new Action(() => _discoverer.RegisterService("dawwdawd", new IPEndPoint(IPAddress.Any, 0), new byte[500]))
			.ShouldThrow<ArgumentOutOfRangeException>()
			.WithMessage("The total size of a message may not exceed 512 bytes (this message would be 568 bytes in length)\r\nParameter name: payload");
		}

		[Test]
		[Description("Verifies that when no services are registered, then none can be found")]
		public void TestFindAllServices1()
		{
			var services = _discoverer.FindAllServices();
			services.Should().BeEmpty();
		}

		[Test]
		[Description("Verifies that a service that has been registered is actually returned")]
		public void TestFindServices1()
		{
			const string name = "Foobar";
			var ep = new IPEndPoint(IPAddress.Parse("123.241.108.21"), port: 12345);

			using (_discoverer.RegisterService(name, ep))
			{
				var services = _discoverer.FindServices(name);
				services.Should().NotBeNull();
				services.Should().Contain(new Service(name, ep, IPAddress.Loopback));
			}
		}

		[Test]
		[Description("Verifies that only services with the queried name are returned")]
		public void TestFindServices2()
		{
			const string name = "Foobar";
			var ep = new IPEndPoint(IPAddress.Parse("123.241.108.21"), port: 12345);

			using (_discoverer.RegisterService(name, ep))
			using (_discoverer.RegisterService("Not Foobar", new IPEndPoint(IPAddress.Any, port: 1243)))
			{
				var services = _discoverer.FindServices(name);
				services.Should().NotBeNull();
				services.Should().Contain(new Service(name, ep, IPAddress.Loopback));
			}
		}

		[Test]
		[Description("Verifies that once a service is unregistered, it is no longer found")]
		public void TestFindServices3()
		{
			const string name = "Foobar";
			var ep1 = new IPEndPoint(IPAddress.Parse("123.241.108.21"), port: 12345);
			var ep2 = new IPEndPoint(IPAddress.Parse("1.2.3.4"), port: 1243);

			using (_discoverer.RegisterService(name, ep1))
			{
				List<Service> services;

				using (_discoverer.RegisterService(name, ep2))
				{
					services = _discoverer.FindServices(name);
					services.Should().NotBeNull();
					services.Select(x => x.EndPoint).Distinct().Count().Should().Be(expected: 2,
						because: "Because we should've received responses for 2 different services, but over all possible adapters");
					services.Should().Contain(new Service(name, ep1, IPAddress.Loopback));

					var service = services.First(x => !Equals(x.EndPoint, ep1));
					service.EndPoint.Should().NotBeNull();
					service.EndPoint.Address.Should().NotBe(IPAddress.Any,
						"Because a specific address should be given in the response");
					service.Name.Should().Be(name);
				}

				services = _discoverer.FindServices(name);
				services.Should().NotBeNull();
				services.Should().Contain(new Service(name, ep1, IPAddress.Loopback));
			}
		}

		[Test]
		[Description(
			"Verifies that services which are bound to any address are responded with all explicit addresses of the particular machine")]
		public void TestFindServices4()
		{
			const string name = "Foobar";
			var ep = new IPEndPoint(IPAddress.Any, port: 12345);

			using (_discoverer.RegisterService(name, ep))
			{
				var services = _discoverer.FindServices(name);
				services.Should().NotBeNull();

				var iPv4Interfaces = NetworkInterface.GetAllNetworkInterfaces()
					.Where(x => x.OperationalStatus == OperationalStatus.Up)
					.SelectMany(x => x.GetIPProperties().UnicastAddresses)
					.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
					.ToList();
				services.Count.Should().Be(iPv4Interfaces.Count);
			}
		}

		[Test]
		[Description("Verifies that a service registered with one discoverer can be found via another")]
		public void TestFindServices5()
		{
			var payload = Encoding.UTF8.GetBytes("Version 5, Protocol 120");

			using (var registry = new NetworkServiceDiscoverer())
			using (registry.RegisterService("MyAwesomeWebApplication",
				new IPEndPoint(IPAddress.Parse("19.87.0.12"), port: 15431), payload))
			{
				var services = _discoverer.FindServices("MyAwesomeWebApplication");
				services.Should().NotBeNull();
				services.Should().NotBeEmpty();
				foreach (var service in services)
				{
					service.Name.Should().Be("MyAwesomeWebApplication");
					service.Payload.Should().Equal(payload);
				}
			}
		}

		[Test]
		[Description("Verifies that if both legacy and non legacy responses are received, then duplicate legacy responses are filtered out")]
		public void TestFindServices6()
		{
			var payload = Encoding.UTF8.GetBytes("Version 5, Protocol 120");
			using (var registry = new NetworkServiceDiscoverer(sendLegacyResponse: true))
			using (registry.RegisterService("MyAwesomeWebApplication",
				new IPEndPoint(IPAddress.Parse("19.87.0.12"), port: 15431), payload))
			{
				var services = _discoverer.FindServices("MyAwesomeWebApplication");
				services.Should().NotBeNull();
				services.Should().NotBeEmpty();
				foreach (var service in services)
				{
					service.Name.Should().Be("MyAwesomeWebApplication");
					service.Payload.Should().Equal(payload);
				}
			}
		}
	}
}