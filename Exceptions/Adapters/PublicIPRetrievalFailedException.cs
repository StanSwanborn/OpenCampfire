namespace OpenCampfire.Exceptions.Adapters;

public class PublicIpRetrievalFailedException : Exception
{
    public PublicIpRetrievalFailedException() : base("The request to retrieve the public IP of the current host failed")
    {}
}