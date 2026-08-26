public interface IAuthorizationTokenStore
{
    AuthorizationToken? Load();

    void Save(AuthorizationToken token);

    void Clear();
}