// ReSharper disable once CheckNamespace

namespace SharpRemote
{
	/// <summary>
	/// </summary>
	public interface IEnumValueDescription
	{
		/// <summary>
		///     The name of the enum value.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     The actual enum value.
		/// </summary>
		object Value { get; }

		/// <summary>
		///     The numeric value of the enum value.
		///     Might be false in case the enum's storage type is <see cref="ulong" />.
		/// </summary>
		long NumericValue { get; }
	}
}