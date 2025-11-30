namespace AuthService.Models
{
    public record ChangeEmailRequest
    (
        string UserId,
        string NewEmail,
        string CurrentPassword
    );
}
