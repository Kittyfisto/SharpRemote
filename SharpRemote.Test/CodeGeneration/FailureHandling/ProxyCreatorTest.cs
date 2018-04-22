using System;
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
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
			                                                          AssemblyBuilderAccess.RunAndSave);
			string moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(moduleName);
		}

		public void Save()
		{
			var fname = "SharpRemote.GeneratedCode.FaultTolerance.dll";
			try
			{
				_assembly.Save(fname);
				TestContext.Out.WriteLine("Assembly written to: {0}", Path.Combine(Directory.GetCurrentDirectory(), fname));
			}
			catch (Exception e)
			{
				TestContext.Out.WriteLine("Couldn't write assembly: {0}", e);
			}
		}

		#region Fallbacks

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
			Save();

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
				.ShouldThrow<AggregateException>()
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
			Save();

			new Action(() => Await(proxy.DoStuff()))
				.ShouldThrow<AggregateException>()
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
				.ShouldThrow<AggregateException>()
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
			Save();

			new Action(() => Await(proxy.DoStuff()))
				.ShouldThrow<AggregateException>()
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
				.ShouldThrow<ArgumentNullException>();
			new Action(() => creator.PrepareProxyFor<IVoidMethod>(null)
			                        .WithFallbackTo(fallback.Object)
			                        .Create())
				.ShouldThrow<ArgumentNullException>();
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
			subject.Setup(x => x.Do()).Returns(1337);
			var fallback = new Mock<IInt32Method>();
			fallback.Setup(x => x.Do()).Returns(42);

			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithFallbackTo(fallback.Object)
			                   .Create();
			proxy.Do().Should().Be(1337);

			subject.Setup(x => x.Do()).Throws<NullReferenceException>();
			proxy.Do().Should().Be(42);
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
			Save();

			proxy.DoStuff();
			subject.Verify(x => x.DoStuff(), Times.Once);

			subject.Setup(x => x.DoStuff()).Throws<ArithmeticException>();
			new Action(() => proxy.DoStuff().Wait()).ShouldNotThrow();
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
			new Action(() => proxy.DoStuff()).ShouldNotThrow();
			subject.Verify(x => x.DoStuff(), Times.Exactly(2));
		}

		[Test]
		public void TestIInt32MethodDefaultFallback()
		{
			var subject = new Mock<IInt32Method>();
			subject.Setup(x => x.Do()).Returns(32412);

			var creator = Create();
			var proxy = creator.PrepareProxyFor(subject.Object)
			                   .WithDefaultFallback()
			                   .Create();

			proxy.Do().Should().Be(32412);

			subject.Setup(x => x.Do()).Throws<ArithmeticException>();
			proxy.Do().Should().Be(0);
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
			new Action(() => proxy.Do(long.MinValue)).ShouldNotThrow();
			subject.Verify(x => x.Do(long.MinValue), Times.Once);
		}

		#endregion
	}
}
