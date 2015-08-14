using System.Collections.Generic;
using System.Net;
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
			var ep2 = new IPEndPoint(IPAddress.Any, 1243);

			using (P2P.RegisterService(name, ep1))
			{
				List<Service> services;

				using (P2P.RegisterService(name, ep2))
				{
					services = P2P.FindServices(name);
					services.Should().NotBeNull();
					services.Should().BeEquivalentTo(new[]
						{
							new Service(name, ep1),
							new Service(name, ep2)
						});
				}

				services = P2P.FindServices(name);
				services.Should().NotBeNull();
				services.Should().BeEquivalentTo(new[]
						{
							new Service(name, ep1),
						});
			}
		}
	}
}