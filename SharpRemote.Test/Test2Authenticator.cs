namespace SharpRemote.Test
{
	/// <summary>
	/// This authenticator is crafted to fail the challenge set forward by <see cref="TestAuthenticator"/>.
	/// </summary>
	internal sealed class Test2Authenticator
		: IAuthenticator
	{
		public string CreateChallenge()
		{
			return "B";
		}

		public string CreateResponse(string challenge)
		{
			return challenge + "foo";
		}

		public bool Authenticate(string challenge, string response)
		{
			return response == challenge + "foo";
		}
	}
}