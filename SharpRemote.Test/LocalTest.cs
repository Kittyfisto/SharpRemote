using NUnit.Framework;

namespace SharpRemote.Test
{
	/// <summary>
	/// Attribute to mark tests that wont run in the CI environment (missing dependencies, rights, etc..)
	/// </summary>
	public sealed class LocalTest
		: CategoryAttribute
	{
		public LocalTest(string description)
			: base("LocalTest")
		{
			
		}
	}
}