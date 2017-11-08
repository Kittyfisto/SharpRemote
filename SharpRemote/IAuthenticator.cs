namespace SharpRemote
{
	/// <summary>
	/// The interface used to perform a challenge-response authentication between
	/// client and server.
	/// </summary>
	public interface IAuthenticator
	{
		/// <summary>
		/// Starts a new challenge.
		/// </summary>
		/// <returns></returns>
		string CreateChallenge();

		/// <summary>
		/// Creates the response for the given challenge.
		/// </summary>
		/// <param name="challenge"></param>
		/// <returns></returns>
		string CreateResponse(string challenge);

		/// <summary>
		/// Performs the authentication for the given challenge and response.
		/// </summary>
		/// <param name="challenge"></param>
		/// <param name="response"></param>
		/// <returns>True when the challenge succeeded, false otherwise</returns>
		bool Authenticate(string challenge, string response);
	}
}