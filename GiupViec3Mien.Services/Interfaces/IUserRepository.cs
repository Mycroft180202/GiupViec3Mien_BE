using GiupViec3Mien.Domain.Entities;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByPhoneAsync(string phone);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
