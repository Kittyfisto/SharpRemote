using System;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Responsible for creating & hosting objects.
	/// </summary>
	public interface ISubjectHost
		: IDisposable
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		ulong CreateSubject(Type type, Type interfaceType);
	}
}