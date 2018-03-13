using System;

namespace SharpRemote.EndPoints
{
	[Flags]
	internal enum MessageType : byte
	{
		Call = 0x1,
		Return = 0x2,
		Exception = 0x4,
		Goodbye = 0x8,

		None = 0
	}
}