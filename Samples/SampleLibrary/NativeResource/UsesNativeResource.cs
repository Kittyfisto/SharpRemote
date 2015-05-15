using System;

namespace SampleLibrary.NativeResource
{
	/// <summary>
	/// An example class that uses a native resource that, due to depending on
	/// a legacy software, can only be executed within an x86 environment.
	/// </summary>
	public sealed class UsesNativeResource
		: IUsesNativeResource
	{
		public UsesNativeResource()
		{
			if (Environment.Is64BitProcess)
				throw new NotSupportedException("Can only executed under 32bit!");
		}

		public void Load(string path)
		{
			throw new NotImplementedException();
		}

		public string CurrentPath
		{
			get { throw new NotImplementedException(); }
		}

		public double CalculateTheMeaningOfEverything()
		{
			throw new NotImplementedException();
		}

		public Metadata Metadata
		{
			get { throw new NotImplementedException(); }
		}
	}
}