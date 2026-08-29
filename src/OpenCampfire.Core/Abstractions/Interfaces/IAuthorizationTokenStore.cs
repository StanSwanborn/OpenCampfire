using OpenCampfire.Abstractions.Models;

namespace OpenCampfire.Abstractions.Interfaces;

public interface IAuthorizationTokenStore
{
    AuthorizationToken? Load();

    void Save(AuthorizationToken token);

    void Clear();
}