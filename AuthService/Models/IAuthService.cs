namespace AuthService.Models
{
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterRequest request);
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<AuthResponse?> RefreshTokenAsync(TokenRefreshRequest request);
        Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken);
    }
}
