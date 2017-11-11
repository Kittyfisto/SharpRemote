using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	/// </summary>
	[DataContract]
	public enum SpecialMethod
	{
		/// <summary>
		///     The method's not special at all.
		/// </summary>
		[EnumMember] None = 0,

		/// <summary>
		/// </summary>
		[EnumMember] BeforeSerialize = 1,

		/// <summary>
		/// </summary>
		[EnumMember] AfterSerialize = 2,

		/// <summary>
		/// </summary>
		[EnumMember] BeforeDeserialize = 3,

		/// <summary>
		/// </summary>
		[EnumMember] AfterDeserialize = 4
	}
}