namespace SharpRemote.Test
{
	/// <summary>
	///     DO NOT USE THIS AUTHENTICATOR IN PRODUCTION CODE.
	/// </summary>
	internal sealed class TestAuthenticator
		: IAuthenticator
	{
		public string CreateChallenge()
		{
			return "A";
		}

		public string CreateResponse(string challenge)
		{
			return challenge + challenge;
		}

		public bool Authenticate(string challenge, string response)
		{
			return response == challenge + challenge;
		}
	}
}