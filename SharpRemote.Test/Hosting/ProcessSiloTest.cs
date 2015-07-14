using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class ProcessSiloTest
	{
		[Test]
		public void TestCreateGrain1()
		{
			using (var silo = new ProcessSiloClient())
			{
				var proxy = silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
				proxy.Value.Should().Be("Foobar");
			}
		}

		[Test]
		public void TestCtor()
		{
			using (var silo = new ProcessSiloClient())
			{
				silo.IsProcessRunning.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that a crash of the host process is detected when it happens while a method call")]
		public void TestFailureDetection1()
		{
			using (var silo = new ProcessSiloClient())
			{
				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof (KillsProcess));
				new Action(proxy.Do)
					.ShouldThrow<ConnectionLostException>("Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");
			}
		}

		[Test]
		[Description("Verifies that an abortion of the executing thread of a remote method invocation is detected and that it causes a connection loss")]
		public void TestFailureDetection2()
		{
			using (var silo = new ProcessSiloClient())
			{
				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof(KillsProcess));
				new Action(proxy.Do)
					.ShouldThrow<ConnectionLostException>("Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");
			}
		}
	}
}