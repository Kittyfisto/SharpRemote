namespace SharpRemote.CodeGeneration.FaultTolerance.Fallback
{
	internal interface IFallbackProxyCreator
	{
		object Create(object subject, object fallback);
	}
}