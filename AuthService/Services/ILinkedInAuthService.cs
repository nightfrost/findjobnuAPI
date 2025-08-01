using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AuthService.Services
{
    public interface ILinkedInAuthService
    {
        Task<IResult> HandleCallbackAsync(HttpContext context);
        string GetLoginUrl();
    }
}
