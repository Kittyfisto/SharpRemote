namespace SharpRemote.Watchdog
{
	internal interface IIsolatedStorage
	{
		T Restore<T>(string name);
		void Store<T>(string name, T value);
	}
}