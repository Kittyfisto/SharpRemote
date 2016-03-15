using System;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This interface is used to detected failures of remote endpoints.
	/// </summary>
	[ByReference]
	public interface IHeartbeat
	{
		/// <summary>
		///     This method is called when a debugger has been attached to the process of the caller.
		///     If allowed, the callee will disable its heartbeat detection until the next call to
		///     <see cref="RemoteDebuggerDetached" />.
		/// </summary>
		event Action RemoteDebuggerAttached;

		/// <summary>
		///     This method is called when a debugger has been detached from the process of the caller.
		///     If allowed, the callee will enable its heartbeat detection again until the next call to
		///     <see cref="RemoteDebuggerAttached" />.
		/// </summary>
		event Action RemoteDebuggerDetached;

		/// <summary>
		///     Called regularly in order to detect whether or not the remote endpoint is still alive or not.
		/// </summary>
		Task Beat();
	}
}