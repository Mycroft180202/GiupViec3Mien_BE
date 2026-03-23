using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IZaloService
{
    /// <summary>
    /// Sends a Zalo Notification Service (ZNS) template message.
    /// </summary>
    Task<bool> SendZnsMessageAsync(string phoneNumber, string templateId, object templateData);
}

public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message);
}
