using System.Reflection;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="ParameterInfo" /> (in that it describes a particular .NET method), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	public interface IParameterDescription
	{
		/// <summary>
		///     The equivalent of <see cref="ParameterInfo.Name" />.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     Gets a value indicating whether this is an input parameter.
		///     The equivalent of <see cref="ParameterInfo.IsIn" />.
		/// </summary>
		bool IsIn { get; }

		/// <summary>
		///     Gets a value indicating whether this is an output parameter.
		///     The equivalent of <see cref="ParameterInfo.IsOut" />.
		/// </summary>
		bool IsOut { get; }

		/// <summary>
		///     Gets a value indicating whether this is a Retval parameter.
		///     The equivalent of <see cref="ParameterInfo.IsRetval" />.
		/// </summary>
		bool IsRetval { get; }

		/// <summary>
		///     Gets the zero-based position of the parameter in the formal parameter list.
		///     The equivalent of <see cref="ParameterInfo.Position" />.
		/// </summary>
		int Position { get; }

		/// <summary>
		///     The equivalent of <see cref="ParameterInfo.ParameterType" />.
		/// </summary>
		ITypeDescription ParameterType { get; }
	}
}