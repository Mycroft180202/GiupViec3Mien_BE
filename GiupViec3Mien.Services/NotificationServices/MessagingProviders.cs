using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GiupViec3Mien.Services.NotificationServices;

public class ZaloService : Interfaces.IZaloService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZaloService> _logger;

    public ZaloService(HttpClient httpClient, IConfiguration configuration, ILogger<ZaloService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendZnsMessageAsync(string phoneNumber, string templateId, object templateData)
    {
        string accessToken = _configuration["Zalo:AccessToken"] ?? "EMPTY_ACCESS_TOKEN";
        
        // Ensure phone number starts with 84 format required by Zalo
        if (phoneNumber.StartsWith("0")) phoneNumber = "84" + phoneNumber.Substring(1);

        var payload = new
        {
            phone = phoneNumber,
            template_id = templateId,
            template_data = templateData
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Post, "https://business.openapi.zalo.me/message/template");
            request.Headers.Add("access_token", accessToken);
            request.Content = content;

            // In production with a valid access token, actually await _httpClient.SendAsync(request);
            // Since we don't have the token configured yet, we log the payload to simulate successful sending out.
            if (accessToken == "EMPTY_ACCESS_TOKEN")
            {
                _logger.LogWarning($"[ZALO ZNS SIMULATION] Sending Template {templateId} to Phone {phoneNumber} with Data: {JsonSerializer.Serialize(templateData)}. Please configure Zalo:AccessToken in appsettings.json.");
                return true;
            }

            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Zalo ZNS message sent successfully to {phoneNumber}. Response: {responseString}");
                return true;
            }
            else
            {
                _logger.LogError($"Failed to send Zalo ZNS message to {phoneNumber}. Status: {response.StatusCode}. Response: {responseString}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending Zalo ZNS message.");
            return false;
        }
    }
}

public class SmsFallbackService : Interfaces.ISmsService
{
    private readonly ILogger<SmsFallbackService> _logger;

    public SmsFallbackService(ILogger<SmsFallbackService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        _logger.LogInformation($"[SMS SIMULATION] To: {phoneNumber} | Message: {message}");
        return Task.FromResult(true);
    }
}
