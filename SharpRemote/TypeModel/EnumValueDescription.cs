using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	/// Describes a specific value of a particular enum.
	/// </summary>
	[DataContract]
	public sealed class EnumValueDescription
		: IEnumValueDescription
	{
		/// <inheritdoc />
		[DataMember]
		public string Name { get; set; }

		/// <inheritdoc />
		public object Value { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long NumericValue { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0}: {1} ({2})", Name, Value, NumericValue);
		}

		[Pure]
		internal static EnumValueDescription Create(Type storageType, object value, string name)
		{
			long numericValue;
			if (storageType == typeof(ulong))
			{
				var numericUnsignedValue = Convert.ToUInt64(value);
				numericValue = unchecked((long) numericUnsignedValue);
			}
			else
			{
				numericValue = Convert.ToInt64(value);
			}

			return new EnumValueDescription
			{
				Name = name,
				Value = value,
				NumericValue = numericValue
			};
		}
	}
}
