using System;

namespace SharpRemote.CodeGeneration.FaultTolerance
{
	internal interface ITimeoutProxyCreator
	{
		object Create(object subject, TimeSpan maximumMethodLatency);
	}
}