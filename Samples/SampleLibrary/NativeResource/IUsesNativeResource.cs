namespace SampleLibrary.NativeResource
{
	public interface IUsesNativeResource
	{
		void Load(string path);

		string CurrentPath { get; }

		double CalculateTheMeaningOfEverything();

		Metadata Metadata { get; }
	}
}