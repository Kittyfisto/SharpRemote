using System;

namespace SharpRemote.WebApi.Attributes
{
	/// <summary>
	///     Indicates that a parameter is to be extracted from the Uri.
	///     It is expected that the <see cref="RouteAttribute.Template" /> references
	///     this parameter by its index (such as "{0}" if this were the first parameter
	///     of the method).
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class FromUriAttribute
		: Attribute
	{
	}
}