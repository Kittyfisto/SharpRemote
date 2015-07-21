namespace SampleLibrary.NativeResource
{
	public interface IUsesNativeResource
	{
		void Load(string path);

		string CurrentPath { get; }

		double CalculateTheMeaningOfEverything();

		Metadata Metadata { get; }

		/// <summary>
		/// Adds a listener to this object.
		/// </summary>
		/// <remarks>
		/// We tell SharpRemote that the <paramref name="listener"/> parameter is to be marshalled by reference
		/// instead of the default by value. This means that instead of serializing that object, a servant is created
		/// on-the-fly while the actual implementation of this interface is called with a proxy that itself
		/// marshalls all calls to the other side.
		/// </remarks>
		/// <param name="listener"></param>
		void AddListener(IErrorListener listener);

		/// <summary>
		/// Removes the given listener from this object or does nothing if it hasn't been registered.
		/// </summary>
		/// <param name="listener"></param>
		void RemoveListener(IErrorListener listener);
	}
}