using System;

namespace SharpRemote.Test
{
	/// <summary>
	/// This attribute is intended to mark those tests which reproduce a bug/test a bugfix.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public sealed class DefectAttribute
		: Attribute
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="url">The url to the reported bug on github.com</param>
		public DefectAttribute(string url)
		{
			
		}
	}
}