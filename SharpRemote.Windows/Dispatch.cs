using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SharpRemote
{
	[DataContract]
	public enum Dispatch
	{
		/// <summary>
		/// Methods are not serialized and may be called in parallel, if and when
		/// the <see cref="TaskScheduler.Default"/> deems it necessary.
		/// </summary>
		/// <remarks>
		/// This is the default mode (as this is how every .NET method is invoked).
		/// </remarks>
		/// <remarks>
		/// Concurrent calls to an implementation method (on the same object) occur only under any of the following conditions:
		/// - there are multiple callees of the same method and object through two threads, processes or machines
		/// - the method returns a <see cref="Task"/> or <see cref="Task{T}"/>
		/// 
		/// If neither of those are true then calls to a method (either tagged with this mode or not tagged because it's the default value)
		/// are guarantueed to be synchronous.
		/// </remarks>
		[EnumMember] DoNotSerialize = 0,

		/// <summary>
		/// Methods are serialized on a per object AND per method basis.
		/// There can never be more than one invocation of the tagged method on the same object at any time.
		/// </summary>
		/// <remarks>
		/// This behaves exactly like the synchronized keyword in java. All pending calls are serialized and
		/// executed in sequence.
		/// </remarks>
		[EnumMember] SerializePerMethod = 1,

		/// <summary>
		/// Methods are serialized on a per object basis.
		/// There can never be more than one invocation of ANY tagged method on the same object at any time.
		/// </summary>
		/// <remarks>
		/// This behaves exactly like locking all tagged methods with the same sync root / exclusive lock.
		/// </remarks>
		[EnumMember] SerializePerObject = 2,

		/// <summary>
		/// Methods are serialized on a per type (interface) basis.
		/// There can never be more than one invocation of ANY tagged method on the same TYPE at any time (regardless of object instance).
		/// </summary>
		/// <remarks>
		/// This behaves exactly like locking all tagged methods with the same STATIC sync root / exclusive lock.
		/// </remarks>
		[EnumMember] SerializePerType = 3,
	}
}