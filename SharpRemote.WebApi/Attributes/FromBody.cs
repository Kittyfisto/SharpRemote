using System;

// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This attribute should be used to indicate which parameter of a method with multiple
	///     parameters should be deserialized from the body of the HTTP request.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class FromBodyAttribute
		: Attribute
	{
	}
}