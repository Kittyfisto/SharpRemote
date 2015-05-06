using System;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Responsible for creating & providing grains.
	/// </summary>
	public interface ISilo
		: IDisposable
	{
		TInterface CreateGrain<TInterface>(Type implementation)
			where TInterface : class;
	}
}