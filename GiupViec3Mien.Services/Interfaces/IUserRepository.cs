using GiupViec3Mien.Domain.Entities;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByPhoneAsync(string phone);
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<User>> GetAllWorkersAsync();
    Task<IEnumerable<User>> GetPublicWorkersAsync();
    Task<IEnumerable<User>> GetAllAsync();
    Task AddAsync(User user);
    Task DeleteAsync(User user);
    void Update(User user);
    Task SaveChangesAsync();
}
