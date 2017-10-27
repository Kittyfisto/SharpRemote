using System.Reflection;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="PropertyInfo" /> (in that it describes a particular .NET property), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	public interface IPropertyDescription
	{
		/// <summary>
		///     The type of this property, equivalent of <see cref="PropertyInfo.PropertyType" />.
		/// </summary>
		TypeDescription PropertyType { get; }

		/// <summary>
		///     The equivalent of <see cref="MemberInfo.Name" />.
		/// </summary>
		string Name { get; }
	}
}