using System;
using System.Threading;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Diagnostics;

namespace SharpRemote.Test
{
	[TestFixture]
	public sealed class HeartbeatTest
	{
		[SetUp]
		public void SetUp()
		{
			_debugger = new Mock<IDebugger>();
			_remotingEndPoint = new Mock<IRemotingEndPoint>();
			_heartbeat = new Heartbeat(_debugger.Object, _remotingEndPoint.Object, true);
		}

		[TearDown]
		public void TearDown()
		{
			if (_heartbeat != null)
				_heartbeat.Dispose();
		}

		private Mock<IDebugger> _debugger;
		private Mock<IRemotingEndPoint> _remotingEndPoint;
		private Heartbeat _heartbeat;

		[Test]
		public void TestDispose()
		{
			_heartbeat.IsDisposed.Should().BeFalse();

			_heartbeat.Dispose();
			_heartbeat.IsDisposed.Should().BeTrue();
		}

		[Test]
		[Description(
			"Verifies that the RemoteDebuggerAttached event is fired when a debugger becomes attached AND the endpoint is connected"
			)]
		public void TestIsDebuggerAttachedEvent()
		{
			using (var attachedEvent = new ManualResetEvent(false))
			using (var detachedEvent = new ManualResetEvent(false))
			{
				_heartbeat.RemoteDebuggerAttached += () => { attachedEvent.Set(); };
				_heartbeat.RemoteDebuggerDetached += () => { detachedEvent.Set(); };

				// Currently, firing an event on the proxy of an unconnected endpoint throws an exception.
				// This is undesirable for fire & forget events (such as this one) and therefore the
				// event is only truly fired when the endpoint is actually connected.
				_remotingEndPoint.Setup(x => x.IsConnected).Returns(true);
				_debugger.Setup(x => x.IsDebuggerAttached).Returns(true);
				attachedEvent.WaitOne(TimeSpan.FromSeconds(1)).Should().BeTrue(
					"Because a debugger attached event should've been reported"
					);

				_debugger.Setup(x => x.IsDebuggerAttached).Returns(false);
				detachedEvent.WaitOne(TimeSpan.FromSeconds(1)).Should().BeTrue(
					"Because a debugger detached event should've been reported"
					);
			}
		}
	}
}