namespace SharpRemote.Diagnostics
{
	internal sealed class Debugger
		: IDebugger
	{
		public static readonly Debugger Instance;

		static Debugger()
		{
			Instance = new Debugger();
		}

		private Debugger()
		{
		}

		public bool IsDebuggerAttached => System.Diagnostics.Debugger.IsAttached;
	}
}