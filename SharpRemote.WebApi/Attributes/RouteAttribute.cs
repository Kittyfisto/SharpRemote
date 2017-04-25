using System;

// ReSharper disable CheckNamespace
namespace SharpRemote.WebApi
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This attribute should be used to describe the route that should be taken
	///     to reach a specific method of a resource controller.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class RouteAttribute
		: Attribute
	{
		/// <summary>
		/// </summary>
		public RouteAttribute()
		{
		}

		/// <summary>
		/// </summary>
		/// <param name="template"></param>
		public RouteAttribute(string template)
		{
			Template = template;
		}

		/// <summary>
		/// </summary>
		public string Template { get; set; }
	}
}