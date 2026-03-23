using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace GiupViec3Mien.Services.NotificationServices;

public class VerificationService : Interfaces.IVerificationService
{
    private readonly IMemoryCache _cache;
    private readonly Interfaces.IZaloService _zaloService;
    private readonly Interfaces.ISmsService _smsService; // Mock sms if needed

    public VerificationService(IMemoryCache cache, Interfaces.IZaloService zaloService, Interfaces.ISmsService smsService)
    {
        _cache = cache;
        _zaloService = zaloService;
        _smsService = smsService;
    }

    public async Task<string> GenerateAndSendOtpAsync(string phoneNumber)
    {
        // 1. Generate 6-digit OTP
        string otpCode = new Random().Next(100000, 999999).ToString();
        
        // 2. Cache it for 5 minutes
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
        
        // Key format: "otp_0901234567"
        _cache.Set($"otp_{phoneNumber}", otpCode, cacheOptions);

        // 3. Dispatch to messaging providers concurrently
        // SMS Fallback
        var smsTask = _smsService.SendSmsAsync(phoneNumber, $"Ma xac minh GiupViec3Mien cua ban la: {otpCode}. Hieu luc trong 5 phut.");
        
        // Zalo ZNS
        // Requires Template ID mapped to {{otp}} template data
        var zaloTask = _zaloService.SendZnsMessageAsync(phoneNumber, "123456", new { otp = otpCode });

        await Task.WhenAll(smsTask, zaloTask);

        return otpCode;
    }

    public bool VerifyOtp(string phoneNumber, string otpCode)
    {
        if (_cache.TryGetValue($"otp_{phoneNumber}", out string? cachedOtp))
        {
            if (cachedOtp == otpCode)
            {
                // Invalidate after successful use to prevent replay attacks
                _cache.Remove($"otp_{phoneNumber}");
                return true;
            }
        }
        return false;
    }
}
