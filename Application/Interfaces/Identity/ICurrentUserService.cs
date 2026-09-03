public interface ICurrentUserService
{
    Task<CurrentUserData> GetCurrent();
}