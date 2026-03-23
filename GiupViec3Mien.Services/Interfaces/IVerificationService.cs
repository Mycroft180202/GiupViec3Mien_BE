using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IVerificationService
{
    Task<string> GenerateAndSendOtpAsync(string phoneNumber);
    bool VerifyOtp(string phoneNumber, string otpCode);
}
