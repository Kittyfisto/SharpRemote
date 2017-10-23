using System;
using System.Threading.Tasks;

namespace SharpRemote
{
	/// <summary>
	///     Can be used to specify how a certain event, method or property should be invoked when being called through a proxy
	///     / servant.
	///     By default, all methods will be invoked on a new task on the <see cref="TaskScheduler.Default" />, e.g. multiple
	///     (concurrent) calls
	///     will be executed on (possibly) multiple threads.
	///     When such a behaviour is not desired, one of the following scheduling types can be used instead:
	///     - <see cref="Dispatch.SerializePerMethod" />: Concurrent invocations of this method (or property/event) are
	///     serialized and executed one after the other
	///     - <see cref="Dispatch.SerializePerObject" />: Concurrent invocations of this method AND any other on the SAME
	///     object with the same invokation-type are serialized and executed one after the other
	///     - <see cref="Dispatch.SerializePerType" />: Concurrent invocations of this method AND any other method on the SAME
	///     interface with the same invokation-type are serialized and executed one after the other
	/// </summary>
	[AttributeUsage(AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property)]
	public class InvokeAttribute
		: Attribute
	{
		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="dispatch"></param>
		public InvokeAttribute(Dispatch dispatch)
		{
			DispatchingStrategy = dispatch;
		}

		/// <summary>
		///     The precise strategy with which the attributed method shall be invoked.
		/// </summary>
		public Dispatch DispatchingStrategy { get; set; }
	}
}