using System.Reflection;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="FieldInfo" /> (in that it describes a particular .NET field), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	public interface IFieldDescription
	{
		/// <summary>
		///     The type of this field, equivalent of <see cref="FieldInfo.FieldType" />.
		/// </summary>
		TypeDescription FieldType { get; }

		/// <summary>
		///     The equivalent of <see cref="MemberInfo.Name" />.
		/// </summary>
		string Name { get; }
	}
}