using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.EndPoints;
using SharpRemote.Test;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.SystemTest.EndPoints
{
	[TestFixture]
	public sealed class ServantStorageTest
	{
		private IRemotingEndPoint _remotingEndPoint;
		private IEndPointChannel _endPointChannel;
		private GrainIdGenerator _idGenerator;
		private ICodeGenerator _codeGenerator;

		[SetUp]
		public void Setup()
		{
			_remotingEndPoint = new Mock<IRemotingEndPoint>().Object;
			_endPointChannel = new Mock<IEndPointChannel>().Object;
			_idGenerator = new GrainIdGenerator(EndPointType.Client);
			_codeGenerator = new Mock<ICodeGenerator>().Object;
		}

		[Test]
		[LocalTest("Requires too many resources to be run on AppVeyor")]
		public void TestConcurrentAccess()
		{
			const int numOperations = 10000;

			using (var storage = CreateStorage())
			{
				var task1 = Task.Factory.StartNew(() =>
				{
					for (uint i = 0; i < numOperations; ++i)
					{
						var subject = new Mock<IGetDoubleProperty>();
						storage.CreateServant(i, subject.Object);
					}
				}, TaskCreationOptions.LongRunning);

				var task2 = Task.Factory.StartNew(() =>
				{
					for (uint i = 0; i < numOperations; ++i)
					{
						var subject = new Mock<IGetFloatProperty>();
						storage.GetExistingOrCreateNewServant(subject.Object);
					}
				}, TaskCreationOptions.LongRunning);

				var task3 = Task.Factory.StartNew(() =>
				{
					for (uint i = 0; i < numOperations; ++i)
					{
						storage.RemoveUnusedServants();
					}
				}, TaskCreationOptions.LongRunning);

				var task4 = Task.Factory.StartNew(() =>
				{
					for (uint i = 0; i < numOperations; ++i)
					{
						storage.RetrieveSubject<IGetFloatProperty>(i);
					}
				}, TaskCreationOptions.LongRunning);

				var task5 = Task.Factory.StartNew(() =>
				{
					for (uint i = 0; i < numOperations; ++i)
					{
						IServant unused1;
						int unused2;
						storage.TryGetServant(i, out unused1, out unused2);
					}
				}, TaskCreationOptions.LongRunning);

				var tasks = new[] {task1, task2, task3, task4, task5};
				new Action(() => Task.WaitAll(tasks)).ShouldNotThrow();
			}
		}

		private ServantStorage CreateStorage()
		{
			return new ServantStorage(_remotingEndPoint,
			                          _endPointChannel,
			                          _idGenerator,
			                          _codeGenerator);
		}
	}
}
