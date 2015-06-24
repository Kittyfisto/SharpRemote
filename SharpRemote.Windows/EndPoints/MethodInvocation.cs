using System;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     Represents a currently executing or pending method invocation.
	/// </summary>
	internal struct MethodInvocation
		: IEquatable<MethodInvocation>
	{
		/// <summary>
		/// 
		/// </summary>
		public readonly long RpcId;

		/// <summary>
		/// The grain that on which a method (or event) is to be executed.
		/// </summary>
		public readonly IGrain Grain;

		/// <summary>
		/// The name of the method that is about to be executed.
		/// </summary>
		public readonly string MethodName;

		/// <summary>
		/// The task that actually executes the method.
		/// </summary>
		public readonly Task Task;

		public MethodInvocation(long rpcId, IGrain grain, string methodName, Task task)
		{
			RpcId = rpcId;
			Grain = grain;
			MethodName = methodName;
			Task = task;
		}

		public override string ToString()
		{
			return string.Format("#{0}: {1} ({2})", RpcId, MethodName, Task.Status);
		}

		public bool Equals(MethodInvocation other)
		{
			return RpcId == other.RpcId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is MethodInvocation && Equals((MethodInvocation) obj);
		}

		public override int GetHashCode()
		{
			return RpcId.GetHashCode();
		}

		public static bool operator ==(MethodInvocation left, MethodInvocation right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(MethodInvocation left, MethodInvocation right)
		{
			return !left.Equals(right);
		}
	}
}