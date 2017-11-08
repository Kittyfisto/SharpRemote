using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="MethodInfo" /> (in that it describes a particular .NET method), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	public interface IMethodDescription
	{
		/// <summary>
		///     The equivalent of <see cref="MemberInfo.Name" />.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     The equivalent of <see cref="MethodInfo.ReturnParameter" />.
		/// </summary>
		IParameterDescription ReturnParameter { get; }

		/// <summary>
		///     The equivalent of <see cref="MethodInfo.ReturnType" />.
		/// </summary>
		ITypeDescription ReturnType { get; }

		/// <summary>
		///     The equivalent of <see cref="MethodBase.GetParameters" />.
		/// </summary>
		IReadOnlyList<IParameterDescription> Parameters { get; }
	}
}