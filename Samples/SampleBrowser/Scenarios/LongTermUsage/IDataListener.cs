using SharpRemote;
using SharpRemote.Attributes;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	/// <summary>
	/// Example interface that is always marshalled by reference instead of the
	/// default "by value".
	/// </summary>
	[ByReference]
	public interface IDataListener
	{
		[Invoke(Dispatch.SerializePerMethod)]
		[AsyncRemote]
		void Process(object data);
	}
}