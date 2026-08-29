using OpenCampfire.Authorization;

namespace OpenCampfire.Abstractions;

public interface IAuthorizationTokenStore
{
    AuthorizationToken? Load();

    void Save(AuthorizationToken token);

    void Clear();
}