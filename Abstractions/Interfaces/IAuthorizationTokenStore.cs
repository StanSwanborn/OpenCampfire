using OpenCampfire.Authorization;

namespace OpenCampfire.Abstractions.Interfaces;

public interface IAuthorizationTokenStore
{
    AuthorizationToken? Load();

    void Save(AuthorizationToken token);

    void Clear();
}