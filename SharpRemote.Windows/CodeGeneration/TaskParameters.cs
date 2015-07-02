using System.IO;

namespace SharpRemote.CodeGeneration
{
	/// <summary>
	/// Used to capture the context that's given to the newly created task
	/// for async operations.
	/// </summary>
	public sealed class TaskParameters
	{
		public readonly string MethodName;
		public readonly MemoryStream Stream;

		public TaskParameters(string methodName, MemoryStream stream)
		{
			MethodName = methodName;
			Stream = stream;
		}
	}
}