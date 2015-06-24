using System;

namespace SharpRemote
{
	/// <summary>
	///     Indicates that a method or a readable property be a factory method to produce
	///     instance of the type it's attached to.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	public sealed class SingletonFactoryMethodAttribute
		: Attribute
	{
	}
}