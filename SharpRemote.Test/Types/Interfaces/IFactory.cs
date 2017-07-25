namespace SharpRemote.Test.Types.Interfaces
{
	[ByReference]
	public interface IFactory
	{
		IByReferenceType Create();
		void Remove(IByReferenceType type);
	}
}