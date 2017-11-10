using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// Responsible for compiling a single serialization method for a particular type.
	/// </summary>
	public interface IMethodCompiler
	{
		/// <summary>
		/// 
		/// </summary>
		MethodBuilder Method { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="methods">All serialization methods of the type being compiled</param>
		/// <param name="methodStorage">Serialization methods for any other available type</param>
		void Compile(AbstractMethodCompiler methods, ISerializationMethodStorage<AbstractMethodCompiler> methodStorage);
	}
}