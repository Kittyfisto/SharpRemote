using System.Diagnostics.Contracts;

namespace SharpRemote.Diagnostics
{
	/// <summary>
	/// Provides access to dot net's debugger.
	/// </summary>
	public interface IDebugger
	{
		/// <summary>
		/// Tests if the debugger is currently attached.
		/// </summary>
		[Pure]
		bool IsDebuggerAttached { get; }
	}
}