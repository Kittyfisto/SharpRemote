using System.Reflection;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="PropertyInfo" /> (in that it describes a particular .NET property), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	public interface IPropertyDescription
		: IMemberDescription
	{
		/// <summary>
		///     The type of this property, equivalent of <see cref="PropertyInfo.PropertyType" />.
		/// </summary>
		ITypeDescription PropertyType { get; }

		/// <summary>
		///     The method through which the value of this property can be accessed.
		///     Equivalent of <see cref="PropertyInfo.GetMethod" />.
		/// </summary>
		IMethodDescription GetMethod { get; }

		/// <summary>
		///     The method through which the value of this property can be changed.
		///     Equivalent of <see cref="PropertyInfo.SetMethod" />.
		/// </summary>
		IMethodDescription SetMethod { get; }
	}
}