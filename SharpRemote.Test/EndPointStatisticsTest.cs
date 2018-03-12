using Moq;
using NUnit.Framework;

namespace SharpRemote.Test
{
	/// <summary>
	/// 
	/// </summary>
	[TestFixture]
	public sealed class EndPointStatisticsTest
	{
		private Mock<IRemotingEndPoint> _endPoint;

		[SetUp]
		public void Setup()
		{
			_endPoint = new Mock<IRemotingEndPoint>();
		}
	}
}