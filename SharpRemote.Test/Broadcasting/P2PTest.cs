using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Broadcasting;

namespace SharpRemote.Test.Broadcasting
{
	[TestFixture]
	public sealed class P2PTest
	{
		[Test]
		[Description("Verifies that when no services are registered, then none can be found")]
		public void TestFindAllServices1()
		{
			var services = P2P.FindAllServices();
			services.Should().BeEmpty();
		}

		[Test]
		[Description("Verifies that a service that has been registered is actually returned")]
		public void TestFindServices1()
		{
			const string name = "Foobar";
			var ep = new IPEndPoint(IPAddress.Parse("123.241.108.21"), 12345);

			using (P2P.RegisterService(name, ep))
			{
				var services = P2P.FindServices(name);
				services.Should().NotBeNull();
				services.Should().Equal(new[]
					{
						new Service(name, ep)
					});
			}
		}

		[Test]
		[Description("Verifies that only services with the queried name are returned")]
		public void TestFindServices2()
		{
			const string name = "Foobar";
			var ep = new IPEndPoint(IPAddress.Parse("123.241.108.21"), 12345);

			using (P2P.RegisterService(name, ep))
			using (P2P.RegisterService("Not Foobar", new IPEndPoint(IPAddress.Any, 1243)))
			{
				var services = P2P.FindServices(name);
				services.Should().NotBeNull();
				services.Should().Equal(new[]
					{
						new Service(name, ep)
					});
			}
		}

		[Test]
		[Description("Verifies that once a service is unregistered, it is no longer found")]
		public void TestFindServices3()
		{
			const string name = "Foobar";
			var ep1 = new IPEndPoint(IPAddress.Parse("123.241.108.21"), 12345);
			var ep2 = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1243);

			using (P2P.RegisterService(name, ep1))
			{
				List<Service> services;

				using (P2P.RegisterService(name, ep2))
				{
					services = P2P.FindServices(name);
					services.Should().NotBeNull();
					services.Count.Should().Be(2);
					services.Should().Contain(new Service(name, ep1));

					var service = services.First(x => !Equals(x.EndPoint, ep1));
					service.EndPoint.Should().NotBeNull();
					service.EndPoint.Address.Should().NotBe(IPAddress.Any, "Because a specific address should be given in the response");
					service.Name.Should().Be(name);
				}

				services = P2P.FindServices(name);
				services.Should().NotBeNull();
				services.Should().BeEquivalentTo(new[]
						{
							new Service(name, ep1),
						});
			}
		}

		[Test]
		[Description("Verifies that services which are bound to any address are responded with all explicit addresses of the particular machine")]
		public void TestFindServices4()
		{
			const string name = "Foobar";
			var ep = new IPEndPoint(IPAddress.Any, 12345);

			using (P2P.RegisterService(name, ep))
			{
				var services = P2P.FindServices(name);
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