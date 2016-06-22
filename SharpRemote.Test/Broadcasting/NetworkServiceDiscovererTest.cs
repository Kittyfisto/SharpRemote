using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Broadcasting
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
			var ep = new IPEndPoint(IPAddress.Parse("123.241.108.21"), 12345);

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
			var ep = new IPEndPoint(IPAddress.Parse("123.241.108.21"), 12345);

			using (_discoverer.RegisterService(name, ep))
			using (_discoverer.RegisterService("Not Foobar", new IPEndPoint(IPAddress.Any, 1243)))
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
			var ep1 = new IPEndPoint(IPAddress.Parse("123.241.108.21"), 12345);
			var ep2 = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1243);

			using (_discoverer.RegisterService(name, ep1))
			{
				List<Service> services;

				using (_discoverer.RegisterService(name, ep2))
				{
					services = _discoverer.FindServices(name);
					services.Should().NotBeNull();
					services.Select(x => x.EndPoint).Distinct().Count().Should().Be(2,
						"Because we should've received responses for 2 different services, but over all possible adapters");
					services.Should().Contain(new Service(name, ep1, IPAddress.Loopback));

					var service = services.First(x => !Equals(x.EndPoint, ep1));
					service.EndPoint.Should().NotBeNull();
					service.EndPoint.Address.Should().NotBe(IPAddress.Any, "Because a specific address should be given in the response");
					service.Name.Should().Be(name);
				}

				services = _discoverer.FindServices(name);
				services.Should().NotBeNull();
				services.Should().Contain(new Service(name, ep1, IPAddress.Loopback));
			}
		}

		[Test]
		[Description("Verifies that services which are bound to any address are responded with all explicit addresses of the particular machine")]
		public void TestFindServices4()
		{
			const string name = "Foobar";
			var ep = new IPEndPoint(IPAddress.Any, 12345);

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
	}
}