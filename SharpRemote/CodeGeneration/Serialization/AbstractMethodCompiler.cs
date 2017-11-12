using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Base class for all method compilers.
	/// </summary>
	public abstract class AbstractMethodCompiler
		: IMethodCompiler
	{
		/// <summary>
		/// </summary>
		protected static readonly MethodInfo CultureInfoGetInvariantCulture;

		/// <summary>
		/// </summary>
		protected static readonly MethodInfo StringFormatObject;

		/// <summary>
		/// </summary>
		protected static readonly MethodInfo StringEquals;

		static AbstractMethodCompiler()
		{
			CultureInfoGetInvariantCulture = typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture)).GetMethod;
			StringFormatObject = typeof(string).GetMethod(nameof(string.Format), new[] {typeof(string), typeof(object)});
			StringEquals = typeof(string).GetMethod(nameof(string.Equals), new[] {typeof(string)});
		}

		/// <inheritdoc />
		public abstract MethodBuilder Method { get; }

		/// <inheritdoc />
		public abstract void Compile(AbstractMethodsCompiler methods,
		                             ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage);
	}
}