using System;

namespace SharpRemote
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class ByReferenceAttribute
		: Attribute
	{
	}
}