using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public unsafe class CausesAccessViolation
		: IVoidMethodNoParameters
	{
		public void Do()
		{
			var data = new byte[10];
			fixed (byte* p = data)
			{
				p[1223123121] = 21;
			}
		}
	}
}