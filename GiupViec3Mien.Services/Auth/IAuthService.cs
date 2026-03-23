using GiupViec3Mien.Services.DTOs.Auth;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> GuestCheckoutAsync(GuestCheckoutRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
}
