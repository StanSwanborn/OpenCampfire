namespace OpenCampfire.Exceptions;

public class OAuthFailedException : Exception
{
    public OAuthFailedException() : base("OAuth authorization failed.")
    {
    }
}