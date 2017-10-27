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
		///     The equivalent of <see cref="ParameterInfo.ParameterType" />.
		/// </summary>
		TypeDescription ParameterType { get; }
	}
}