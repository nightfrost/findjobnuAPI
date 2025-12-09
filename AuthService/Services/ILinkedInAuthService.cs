namespace AuthService.Services
{
    public interface ILinkedInAuthService
    {
        Task<IResult> HandleCallbackAsync(HttpContext context);
        string GetLoginUrl();
        Task<IResult> UnlinkLinkedInProfile(string userId);
    }
}
