using System.Collections.Generic;
using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     A surrogate for <see cref="KeyValuePair{TKey,TValue}" /> which allows every <see cref="ISerializer2" />
	///     to serialize those values.
	/// </summary>
	/// <remarks>
	///     You do not need to care about this type at all, unless you are a maintainer of SharpRemote.
	/// </remarks>
	/// <remarks>
	///     TODO: Can the visibility of this type be set to internal? Only generated code needs to care...
	/// </remarks>
	[DataContract]
	[SerializationSurrogateFor(typeof(KeyValuePair<,>))]
	internal struct KeyValuePairSurrogate<TKey, TValue>
	{
		/// <summary>
		///     Equivalent of <see cref="KeyValuePair{TKey,TValue}.Key" />
		/// </summary>
		[DataMember]
		public TKey Key { get; set; }

		/// <summary>
		///     Equivalent of <see cref="KeyValuePair{TKey,TValue}.Value" />
		/// </summary>
		[DataMember]
		public TValue Value { get; set; }

		/// <summary>
		///     Converts a <see cref="KeyValuePair{TKey,TValue}" /> to a surrogate.
		/// </summary>
		/// <param name="that"></param>
		public static explicit operator KeyValuePairSurrogate<TKey, TValue>(KeyValuePair<TKey, TValue> that)
		{
			return new KeyValuePairSurrogate<TKey, TValue>
			{
				Key = that.Key,
				Value = that.Value
			};
		}

		/// <summary>
		///     Converts a surrogate to a <see cref="KeyValuePair{TKey,TValue}" />.
		/// </summary>
		/// <param name="that"></param>
		public static explicit operator KeyValuePair<TKey, TValue>(KeyValuePairSurrogate<TKey, TValue> that)
		{
			return new KeyValuePair<TKey, TValue>(that.Key, that.Value);
		}
	}
}