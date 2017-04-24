namespace SharpRemote.WebApi
{
	/// <summary>
	///     Identifies one of the possible HTTP Methods.
	/// </summary>
	public enum HttpMethod
	{
		/// <summary>
		///     This method should be used to create new resources.
		/// </summary>
		Post,

		/// <summary>
		///     This method should be used to retrieve a resource (or a list thereof).
		/// </summary>
		Get,

		/// <summary>
		///     This method should be used to update or replace a resource.
		/// </summary>
		Put,

		/// <summary>
		///     This method should be used to update or modify a resource.
		/// </summary>
		Patch,

		/// <summary>
		///     This method should be used to delete a resource.
		/// </summary>
		Delete
	}
}