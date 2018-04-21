using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration.FaultTolerance;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.CodeGeneration.FailureHandling
{
	[TestFixture]
	public sealed class FaultTolerantProxyCreatorTest
	{
		private FaultTolerantProxyCreator Create()
		{
			return new FaultTolerantProxyCreator(_module);
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

		[Test]
		public void TestCreateProxyWithFallback()
		{
			var subject = new Mock<IVoidMethod>();
			var fallback = new Mock<IVoidMethod>();

			var creator = Create();
			new Action(() => creator.CreateProxyWithFallback(null, fallback.Object))
				.ShouldThrow<ArgumentNullException>();
			new Action(() => creator.CreateProxyWithFallback(subject.Object, null))
				.ShouldThrow<ArgumentNullException>();
		}

		[Test]
		public void TestIVoidMethodFallback()
		{
			var subject = new Mock<IVoidMethod>();
			var fallback = new Mock<IVoidMethod>();

			var creator = Create();
			var proxy = creator.CreateProxyWithFallback(subject.Object, fallback.Object);
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
			var proxy = creator.CreateProxyWithFallback(subject.Object, fallback.Object);
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
			var proxy = creator.CreateProxyWithFallback(subject.Object, fallback.Object);
			proxy.Do(long.MaxValue);
			subject.Verify(x => x.Do(long.MaxValue), Times.Once);
			fallback.Verify(x => x.Do(It.IsAny<long>()), Times.Never);

			subject.Setup(x => x.Do(long.MinValue)).Throws<AccessViolationException>();
			proxy.Do(long.MinValue);
			subject.Verify(x => x.Do(long.MinValue), Times.Once);
			fallback.Verify(x => x.Do(long.MinValue), Times.Once);
		}

		#endregion
	}
}
