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

		public bool IsDebuggerAttached
		{
			get { return System.Diagnostics.Debugger.IsAttached; }
		}
	}
}