﻿using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration.FaultTolerance;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.CodeGeneration.FailureHandling
{
	[TestFixture]
	public sealed class ProxyCreatorTest
	{
		private ProxyCreator Create()
		{
			return new ProxyCreator(_module);
		}

		private AssemblyBuilder _assembly;
		private ModuleBuilder _module;

		[SetUp]
		public void Setup()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.FaultTolerance");
			_assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			string moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(moduleName);
		}

		#region Timeouts

		[Test]
		[Ignore("Not yet implemented")]
		[Description("Verifies that the method call is forwarded to the subject and its return value forward")]
		public void TestIReturnsIntTaskMethodStringTimeout1()
		{
			var subject = new Mock<IReturnsIntTaskMethodString>();
			subject.Setup(x => x.CreateFile("simon")).Returns(Task.FromResult(-21321211));

			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithMaximumLatencyOf(TimeSpan.FromSeconds(10))
			                   .Create();

			Await(proxy.CreateFile("simon")).Should().Be(-21321211);
			subject.Verify(x => x.CreateFile("simon"), Times.Once);
		}

		#endregion

		#region Fallbacks

		#region Task<int>(string)

		[Test]
		[Description("Verifies that if the subject succeeds, then the fallback is never invoked")]
		public void TestIReturnsIntTaskMethodStringFallback1()
		{
			var subject = new Mock<IReturnsIntTaskMethodString>();
			var fallback = new Mock<IReturnsIntTaskMethodString>();
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			Await(proxy.CreateFile("foo.txt"));
			subject.Verify(x => x.CreateFile("foo.txt"), Times.Once, "because the subject should've been invoked once");
			fallback.Verify(x => x.CreateFile(It.IsAny<string>()), Times.Never, "because the subject's method call succeeded and thus the fallback may not have been invoked");
		}

		[Test]
		[Description("Verifies that exceptions thrown by the subject are ignored and the fallback is invoked instead")]
		public void TestIReturnsIntTaskMethodStringFallback2()
		{
			var subject = new Mock<IReturnsIntTaskMethodString>();
			subject.Setup(x => x.CreateFile("hello_world.bmp")).Throws<ArithmeticException>();
			var fallback = new Mock<IReturnsIntTaskMethodString>();
			fallback.Setup(x => x.CreateFile("hello_world.bmp")).Returns(Task.FromResult(1337));
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			Await(proxy.CreateFile("hello_world.bmp")).Should().Be(1337);
			subject.Verify(x => x.CreateFile("hello_world.bmp"), Times.Once);
			fallback.Verify(x => x.CreateFile("hello_world.bmp"), Times.Once);
		}

		[Test]
		[Description("Verifies that exceptions thrown by the subject's task are ignored and the fallback is invoked instead")]
		public void TestIReturnsIntTaskMethodStringFallback3()
		{
			var subject = new Mock<IReturnsIntTaskMethodString>();
			subject.Setup(x => x.CreateFile("fool.cs")).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			var fallback = new Mock<IReturnsIntTaskMethodString>();
			fallback.Setup(x => x.CreateFile("fool.cs")).Returns(Task.FromResult(5244222));
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			Await(proxy.CreateFile("fool.cs")).Should().Be(5244222);
			subject.Verify(x => x.CreateFile("fool.cs"), Times.Once);
			fallback.Verify(x => x.CreateFile("fool.cs"), Times.Once);
		}

		[Test]
		[Description("Verifies that exceptions thrown by the fallback are forwarded")]
		public void TestIReturnsIntTaskMethodString4()
		{
			var subject = new Mock<IReturnsIntTaskMethodString>();
			subject.Setup(x => x.CreateFile("stalker shadow of chernobyl")).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			var fallback = new Mock<IReturnsIntTaskMethodString>();
			fallback.Setup(x => x.CreateFile("stalker shadow of chernobyl")).Throws<FileNotFoundException>();
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			new Action(() => Await(proxy.CreateFile("stalker shadow of chernobyl")))
				.Should().Throw<AggregateException>()
				.WithInnerException<FileNotFoundException>();
			subject.Verify(x => x.CreateFile("stalker shadow of chernobyl"), Times.Once);
			fallback.Verify(x => x.CreateFile("stalker shadow of chernobyl"), Times.Once);
		}

		[Test]
		[Description("Verifies that exceptions thrown by the fallback's task are forwarded")]
		public void TestIReturnsIntTaskMethodStringFallback5()
		{
			var subject = new Mock<IReturnsIntTaskMethodString>();
			subject.Setup(x => x.CreateFile("great_game")).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			var fallback = new Mock<IReturnsIntTaskMethodString>();
			fallback.Setup(x => x.CreateFile("great_game")).Returns(TaskEx.Failed<int>(new FileNotFoundException()));
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			new Action(() => Await(proxy.CreateFile("great_game")))
				.Should().Throw<AggregateException>()
				.WithInnerException<FileNotFoundException>();
			subject.Verify(x => x.CreateFile("great_game"), Times.Once);
			fallback.Verify(x => x.CreateFile("great_game"), Times.Once);
		}

		#endregion

		#region Task<int>

		[Test]
		[Description("Verifies that if the subject succeeds, then the fallback is never invoked")]
		public void TestIReturnsIntTaskFallback1()
		{
			var subject = new Mock<IReturnsIntTask>();
			var fallback = new Mock<IReturnsIntTask>();
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			Await(proxy.DoStuff());
			subject.Verify(x => x.DoStuff(), Times.Once, "because the subject should've been invoked once");
			fallback.Verify(x => x.DoStuff(), Times.Never, "because the subject's method call succeeded and thus the fallback may not have been invoked");
		}

		[Test]
		[Description("Verifies that exceptions thrown by the subject are ignored and the fallback is invoked instead")]
		public void TestIReturnsIntTaskFallback2()
		{
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Throws<ArithmeticException>();
			var fallback = new Mock<IReturnsIntTask>();
			fallback.Setup(x => x.DoStuff()).Returns(Task.FromResult(1337));
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			Await(proxy.DoStuff()).Should().Be(1337);
			subject.Verify(x => x.DoStuff(), Times.Once);
			fallback.Verify(x => x.DoStuff(), Times.Once);
		}

		[Test]
		[Description("Verifies that exceptions thrown by the subject's task are ignored and the fallback is invoked instead")]
		public void TestIReturnsIntTaskFallback3()
		{
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			var fallback = new Mock<IReturnsIntTask>();
			fallback.Setup(x => x.DoStuff()).Returns(Task.FromResult(5244222));
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			Await(proxy.DoStuff()).Should().Be(5244222);
			subject.Verify(x => x.DoStuff(), Times.Once);
			fallback.Verify(x => x.DoStuff(), Times.Once);
		}

		[Test]
		[Description("Verifies that exceptions thrown by the fallback are forwarded")]
		public void TestIReturnsIntTaskFallback4()
		{
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			var fallback = new Mock<IReturnsIntTask>();
			fallback.Setup(x => x.DoStuff()).Throws<FileNotFoundException>();
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			new Action(() => Await(proxy.DoStuff()))
				.Should().Throw<AggregateException>()
				.WithInnerException<FileNotFoundException>();
			subject.Verify(x => x.DoStuff(), Times.Once);
			fallback.Verify(x => x.DoStuff(), Times.Once);
		}

		[Test]
		[Description("Verifies that exceptions thrown by the fallback's task are forwarded")]
		public void TestIReturnsIntTaskFallback5()
		{
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			var fallback = new Mock<IReturnsIntTask>();
			fallback.Setup(x => x.DoStuff()).Returns(TaskEx.Failed<int>(new FileNotFoundException()));
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			new Action(() => Await(proxy.DoStuff()))
				.Should().Throw<AggregateException>()
				.WithInnerException<FileNotFoundException>();
			subject.Verify(x => x.DoStuff(), Times.Once);
			fallback.Verify(x => x.DoStuff(), Times.Once);
		}

		#endregion

		#region Task

		[Test]
		public void TestIReturnsTaskFallback1()
		{
			var source = new TaskCompletionSource<int>();
			var subject = new Mock<IReturnsTask>();
			subject.Setup(x => x.DoStuff()).Returns(source.Task);
			var fallback = new Mock<IReturnsTask>();
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			var task = proxy.DoStuff();
			task.Wait(TimeSpan.FromMilliseconds(10)).Should().BeFalse("because the actual task didn't finished just yet");

			source.SetResult(42);
			Await(task);
			subject.Verify(x => x.DoStuff(), Times.Once, "because the subject should've been invoked once");
			fallback.Verify(x => x.DoStuff(), Times.Never, "because the subject's method call succeeded and thus the fallback may not have been invoked");
		}

		[Test]
		[Description("Verifies that exceptions thrown by the subject are ignored and the fallback is invoked instead")]
		public void TestIReturnsTaskFallback2()
		{
			var subject = new Mock<IReturnsTask>();
			subject.Setup(x => x.DoStuff()).Throws<ArithmeticException>();
			var source = new TaskCompletionSource<int>();
			var fallback = new Mock<IReturnsTask>();
			fallback.Setup(x => x.DoStuff()).Returns(source.Task);
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			var task = proxy.DoStuff();
			task.Wait(TimeSpan.FromMilliseconds(10)).Should().BeFalse("because the actual task didn't finished just yet");
			
			source.SetResult(42);
			Await(task);
			subject.Verify(x => x.DoStuff(), Times.Once);
			fallback.Verify(x => x.DoStuff(), Times.Once);
		}

		[Test]
		[Description("Verifies that exceptions thrown by the subject's task are ignored and the fallback is invoked instead")]
		public void TestIReturnsTaskFallback3()
		{
			var subject = new Mock<IReturnsTask>();
			subject.Setup(x => x.DoStuff()).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			var source = new TaskCompletionSource<int>();
			var fallback = new Mock<IReturnsTask>();
			fallback.Setup(x => x.DoStuff()).Returns(source.Task);
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			var task = proxy.DoStuff();
			task.Wait(TimeSpan.FromMilliseconds(10)).Should().BeFalse("because the actual task didn't finished just yet");

			source.SetResult(42);
			Await(task);
			subject.Verify(x => x.DoStuff(), Times.Once);
			fallback.Verify(x => x.DoStuff(), Times.Once);
		}

		[Test]
		[Description("Verifies that exceptions thrown by the fallback are forwarded")]
		public void TestIReturnsTaskFallback4()
		{
			var subject = new Mock<IReturnsTask>();
			subject.Setup(x => x.DoStuff()).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			var fallback = new Mock<IReturnsTask>();
			fallback.Setup(x => x.DoStuff()).Throws<FileNotFoundException>();
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			new Action(() => Await(proxy.DoStuff()))
				.Should().Throw<AggregateException>()
				.WithInnerException<FileNotFoundException>();
			subject.Verify(x => x.DoStuff(), Times.Once);
			fallback.Verify(x => x.DoStuff(), Times.Once);
		}

		[Test]
		[Description("Verifies that exceptions thrown by the fallback's task are forwarded")]
		public void TestIReturnTaskFallback5()
		{
			var subject = new Mock<IReturnsTask>();
			subject.Setup(x => x.DoStuff()).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			var fallback = new Mock<IReturnsTask>();
			fallback.Setup(x => x.DoStuff()).Returns(TaskEx.Failed<int>(new FileNotFoundException()));
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();

			new Action(() => Await(proxy.DoStuff()))
				.Should().Throw<AggregateException>()
				.WithInnerException<FileNotFoundException>();
			subject.Verify(x => x.DoStuff(), Times.Once);
			fallback.Verify(x => x.DoStuff(), Times.Once);
		}

		#endregion

		private void Await(Task task)
		{
			task.Should().NotBeNull("because a non-null task should've been returned");
			task.Wait(TimeSpan.FromSeconds(10))
			    .Should().BeTrue("because the task should've finished within 10 seconds");
		}
		
		private T Await<T>(Task<T> task)
		{
			Await((Task) task);
			return task.Result;
		}

		[Test]
		public void TestCreateProxyWithFallback()
		{
			var subject = new Mock<IVoidMethod>();
			var fallback = new Mock<IVoidMethod>();

			var creator = Create();
			

			new Action(() => creator.PrepareProxyFor(subject.Object)
			                        .WithFallbackTo(null)
			                        .Create())
				.Should().Throw<ArgumentNullException>();
			new Action(() => creator.PrepareProxyFor<IVoidMethod>(null)
			                        .WithFallbackTo(fallback.Object)
			                        .Create())
				.Should().Throw<ArgumentNullException>();
		}

		[Test]
		public void TestIVoidMethodFallback()
		{
			var subject = new Mock<IVoidMethod>();
			var fallback = new Mock<IVoidMethod>();

			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();
			proxy.DoStuff();
			subject.Verify(x => x.DoStuff(), Times.Once);
			fallback.Verify(x => x.DoStuff(), Times.Never);

			subject.Setup(x => x.DoStuff()).Throws<NullReferenceException>();
			proxy.DoStuff();
			subject.Verify(x => x.DoStuff(), Times.Exactly(2));
			fallback.Verify(x => x.DoStuff(), Times.Once);
		}

		[Test]
		public void TestIInt32MethodFallback()
		{
			var subject = new Mock<IInt32Method>();
			subject.Setup(x => x.DoStuff()).Returns(1337);
			var fallback = new Mock<IInt32Method>();
			fallback.Setup(x => x.DoStuff()).Returns(42);

			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();
			proxy.DoStuff().Should().Be(1337);

			subject.Setup(x => x.DoStuff()).Throws<NullReferenceException>();
			proxy.DoStuff().Should().Be(42);
		}

		[Test]
		public void TestIVoidMethodInt64ParameterFallback()
		{
			var subject = new Mock<IVoidMethodInt64Parameter>();
			var fallback = new Mock<IVoidMethodInt64Parameter>();

			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();
			proxy.Do(long.MaxValue);
			subject.Verify(x => x.Do(long.MaxValue), Times.Once);
			fallback.Verify(x => x.Do(It.IsAny<long>()), Times.Never);

			subject.Setup(x => x.Do(long.MinValue)).Throws<AccessViolationException>();
			proxy.Do(long.MinValue);
			subject.Verify(x => x.Do(long.MinValue), Times.Once);
			fallback.Verify(x => x.Do(long.MinValue), Times.Once);
		}

		#endregion

		#region Default Fallbacks

		[Test]
		[Ignore("Not yet implemented")]
		public void TestIReturnsIntTaskDefaultFallback()
		{
			var subject = new Mock<IReturnsIntTask>();
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithDefaultFallback()
			                   .Create();
			proxy.DoStuff();
			subject.Verify(x => x.DoStuff(), Times.Once);

			subject.Setup(x => x.DoStuff()).Throws<ArithmeticException>();
			new Action(() => proxy.DoStuff().Wait()).Should().NotThrow();
			subject.Verify(x => x.DoStuff(), Times.Exactly(2));

			subject.Setup(x => x.DoStuff()).Returns(TaskEx.Failed<int>(new NullReferenceException()));
			proxy.DoStuff().Result.Should().Be(0);
		}

		[Test]
		public void TestIVoidMethodDefaultFallback()
		{
			var subject = new Mock<IVoidMethod>();
			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithDefaultFallback()
			                   .Create();

			proxy.DoStuff();
			subject.Verify(x => x.DoStuff(), Times.Once);

			subject.Setup(x => x.DoStuff()).Throws<ArithmeticException>();
			new Action(() => proxy.DoStuff()).Should().NotThrow();
			subject.Verify(x => x.DoStuff(), Times.Exactly(2));
		}

		[Test]
		public void TestIInt32MethodDefaultFallback()
		{
			var subject = new Mock<IInt32Method>();
			subject.Setup(x => x.DoStuff()).Returns(32412);

			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithDefaultFallback()
			                   .Create();

			proxy.DoStuff().Should().Be(32412);

			subject.Setup(x => x.DoStuff()).Throws<ArithmeticException>();
			proxy.DoStuff().Should().Be(0);
		}

		[Test]
		public void TestIVoidMethodInt64ParameterDefaultFallback()
		{
			var subject = new Mock<IVoidMethodInt64Parameter>();

			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithDefaultFallback()
			                   .Create();

			proxy.Do(44141241);
			subject.Verify(x => x.Do(44141241));

			subject.Setup(x => x.Do(long.MinValue)).Throws<ArithmeticException>();
			new Action(() => proxy.Do(long.MinValue)).Should().NotThrow();
			subject.Verify(x => x.Do(long.MinValue), Times.Once);
		}

		#endregion
	}
}
