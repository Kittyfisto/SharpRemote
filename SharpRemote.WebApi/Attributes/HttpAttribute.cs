using System;

// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public abstract class HttpAttribute
		: Attribute
	{
		/// <summary>
		/// 
		/// </summary>
		public string Route { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public abstract HttpMethod Method { get; }
	}
}