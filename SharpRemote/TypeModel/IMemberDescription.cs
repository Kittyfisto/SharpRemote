using System.Reflection;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	/// </summary>
	public interface IMemberDescription
	{
		/// <summary>
		///     The equivalent of <see cref="System.Reflection.MemberInfo.Name" />.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     The type of this field/property.
		/// </summary>
		ITypeDescription TypeDescription { get; }

		/// <summary>
		/// </summary>
		MemberInfo MemberInfo { get; }
	}
}