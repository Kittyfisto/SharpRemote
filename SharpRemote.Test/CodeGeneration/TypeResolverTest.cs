using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.CodeGeneration
{
	[TestFixture]
	public sealed class TypeResolverTest
	{
		[Test]
		[Description("Verifies that GetType() actually resolves to the correct type")]
		public void TestGetType1()
		{
			var name = typeof (string).AssemblyQualifiedName;
			TypeResolver.GetType(name).Should().Be<string>();
		}

		[Test]
		[Description("Verifies that GetType() is thread-safe")]
		public void TestGetType2()
		{
			var types = new[]
				{
					typeof (int),
					typeof (string),
					typeof (IDisposable),
					typeof (IVoidMethod)
				};

			const int numTries = 10000;
			var exceptions = new List<Exception>();
			var threads = Enumerable.Range(0, 16).Select(unused =>
				{
					var thread = new Thread(() =>
						{
							try
							{
								for (int i = 0; i < numTries; ++i)
								{
									var type = types[i % types.Length];
									TypeResolver.GetType(type.AssemblyQualifiedName)
												.Should().Be(type);
								}
							}
							catch (Exception e)
							{
								lock (exceptions)
								{
									exceptions.Add(e);
								}
							}
						});

					thread.Start();
					return thread;
				}).ToList();

			foreach (var thread in threads)
			{
				thread.Join();
			}

			exceptions.Should().BeEmpty();
		}

		[Test]
		[Description("Verifies that GetType() is at least 10 times faster than Type.GetType()")]
		public void TestGetTypePerformance()
		{
			const int num = 100000;
			var name = typeof(IPEndPoint).AssemblyQualifiedName;

			var sw1 = new Stopwatch();
			sw1.Start();
			for (int i = 0; i < num; ++i)
			{
				Type.GetType(name).Should();
			}
			sw1.Stop();

			var sw2 = new Stopwatch();
			sw2.Start();
			for (int i = 0; i < num; ++i)
			{
				TypeResolver.GetType(name);
			}
			sw2.Stop();

			sw2.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(sw1.ElapsedMilliseconds/10.0));
			Console.WriteLine("Type.GetType(): {0}ms", sw1.ElapsedMilliseconds);
			Console.WriteLine("TypeResolver.GetType(): {0}ms", sw2.ElapsedMilliseconds);
		}
	}
}