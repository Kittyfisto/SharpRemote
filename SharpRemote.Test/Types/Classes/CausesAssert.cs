using System;
using System.Runtime.InteropServices;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class CausesAssert
		: IVoidMethodNoParameters
	{
		static CausesAssert()
		{
			IntPtr unused = IntPtr.Zero;
			NativeMethods.LoadLibrary(ref unused, "SharpRemote.Test.Native.dll");
		}

		public void Do()
		{
			produces_assert();
		}

		[DllImport("SharpRemote.Test.Native.dll")]
		private static extern void produces_assert();
	}
}