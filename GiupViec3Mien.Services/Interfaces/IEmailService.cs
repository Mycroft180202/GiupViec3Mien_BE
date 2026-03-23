using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}
