using System;
using System.Runtime.Serialization;

namespace SharpRemote.Attributes
{
	/// <summary>
	///     This attribute can be applied to instance methods which are invoked after an object
	///     of the instance's type was deserialized.
	/// </summary>
	/// <remarks>
	///     A method with this attribute must be publicly accessible, be an instance method (i.e. non-static)
	///     and may only be part of reference types (classes, not structs) with the <see cref="DataContractAttribute" />.
	/// </remarks>
	/// <remarks>
	///     A type may contain exactly one method with these attributes. If a type hierarchy needs one per sub-type,
	///     then it is advised that the base class declares their method virtual (or provide an additional protected virtual method).
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class AfterDeserializeAttribute
		: SerializationMethodAttribute
	{
		/// <inheritdoc />
		public override SpecialMethod Method => SpecialMethod.AfterDeserialize;
	}
}