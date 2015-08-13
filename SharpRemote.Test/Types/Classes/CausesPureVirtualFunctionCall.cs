using System;
using System.Runtime.InteropServices;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class CausesPureVirtualFunctionCall
		: IVoidMethodNoParameters
	{
		static CausesPureVirtualFunctionCall()
		{
			IntPtr unused = IntPtr.Zero;
			NativeMethods.LoadLibrary(ref unused, "SharpRemote.Test.Native.dll");
		}

		public void Do()
		{
			produces_purecall();
		}

		[DllImport("SharpRemote.Test.Native.dll")]
		private static extern void produces_purecall();
	}
}