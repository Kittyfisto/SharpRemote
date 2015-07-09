using System;

namespace SharpRemote.Test.Types.Interfaces
{
	public interface IInvokeAttributeEvents
	{
		event Action NoAttribute;

		[Invoke(Dispatch.DoNotSerialize)]
		event Action DoNotSerialize;

		[Invoke(Dispatch.SerializePerType)]
		event Action SerializePerType;

		[Invoke(Dispatch.SerializePerObject)]
		event Action SerializePerObject1;

		[Invoke(Dispatch.SerializePerObject)]
		event Action SerializePerObject2;

		[Invoke(Dispatch.SerializePerMethod)]
		event Action SerializePerMethod1;

		[Invoke(Dispatch.SerializePerMethod)]
		event Action SerializePerMethod2;
	}
}