using System;

namespace SharpRemote
{
	/// <summary>
	/// Can be used to signal that a specific parameter, return value, class- or interface type shall not be marshalled by value
	/// but by reference instead. This means that instead of serializing the value, a servant will be created (or re-used if it already exists) 
	/// and the other side (caller/callee depending on whether its a parameter or a return value) will obtain a proxy (either newly
	/// created or re-used if one already exists) to the value.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Interface)]
	public class ByReferenceAttribute
		: Attribute
	{
	}
}