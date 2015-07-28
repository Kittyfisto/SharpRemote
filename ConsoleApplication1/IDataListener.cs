using SharpRemote;

namespace ConsoleApplication1
{
	[ByReference]
	public interface IDataListener
	{
		[Invoke(Dispatch.SerializePerObject)]
		void Process(object data);
	}
}