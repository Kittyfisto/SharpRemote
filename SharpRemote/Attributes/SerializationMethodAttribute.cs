using System;

namespace SharpRemote.Attributes
{
	/// <summary>
	/// </summary>
	public abstract class SerializationMethodAttribute
		: Attribute
	{
		/// <summary>
		///     The type of method being attributed.
		/// </summary>
		public abstract SpecialMethod Method { get; }
	}
}