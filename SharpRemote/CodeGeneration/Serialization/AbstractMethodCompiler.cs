using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// </summary>
	public abstract class AbstractMethodCompiler
		: IMethodCompiler
	{
		/// <summary>
		/// </summary>
		protected static readonly MethodInfo CultureInfoGetInvariantCulture;

		static AbstractMethodCompiler()
		{
			CultureInfoGetInvariantCulture = typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture)).GetMethod;
		}

		/// <inheritdoc />
		public abstract MethodBuilder Method { get; }

		/// <inheritdoc />
		public abstract void Compile(AbstractMethodsCompiler methods,
		                             ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage);
	}
}