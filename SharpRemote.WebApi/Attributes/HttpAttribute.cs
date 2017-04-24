using System;

// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class HttpAttribute
		: Attribute
	{
		public string Route { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public abstract HttpMethod Method { get; }
	}
}