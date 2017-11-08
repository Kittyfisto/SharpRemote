using System;
using System.Threading.Tasks;

namespace SharpRemote.Attributes
{
	/// <summary>
	/// This attribute can be applied to methods with a return type of <see cref="Void"/>
	/// in order to indicate that it should behave exactly like a method that returns <see cref="Task"/>:
	/// The method is dispatched asynchronously, however compared to <see cref="Task"/>, there is no
	/// programmatic way to know when the method has finished, nor when it threw an exception.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]
	public sealed class AsyncRemoteAttribute
		: Attribute
	{
	}
}