namespace AuthService.Models
{
    public record DisableAccountRequest
    (
        string UserId,
        string CurrentPassword
    );
}
