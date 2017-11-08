namespace SharpRemote.Watchdog
{
	/// <summary>
	/// 
	/// </summary>
	public interface IIsolatedStorage
	{
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		T Restore<T>(string name);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void Store<T>(string name, T value);
	}
}