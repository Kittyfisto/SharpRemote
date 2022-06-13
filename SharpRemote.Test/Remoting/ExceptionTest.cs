using System;
using System.IO;
using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public sealed class ExceptionTest
	{
		[Test]
		[Description("Verifies that the remote stacktrace of an exception is preserved when serialized/deserialized")]
		public void TestPreserveRemoteStacktrace()
		{
			using (var stream = new MemoryStream())
			{
				using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
				{
					try
					{
						throw new Exception();
					}
					catch (Exception e)
					{
						AbstractEndPoint.WriteException(writer, e);
					}
				}

				stream.Position = 0;

				using (var reader = new BinaryReader(stream))
				{
					try
					{
						throw AbstractEndPoint.ReadException(reader);
					}
					catch (Exception e)
					{
#if NET6_0
						var property = e.GetType().GetProperty("SerializationStackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
#else
						var property = e.GetType().GetProperty("RemoteStackTrace", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
						var remoteStackTrace = (string)property.GetValue(e);
						remoteStackTrace.Should().NotBeEmpty("because the remote stacktrace of the exception should've been preserved");

						var stacktrace = e.StackTrace;
						stacktrace.Should().Contain(remoteStackTrace,
							"because the remote stacktrace should be part of the actual stacktrace to allow for easier debugging of distributed applications (I want to know where it crashed on the server)");
					}
				}
			}
		}
	}
}