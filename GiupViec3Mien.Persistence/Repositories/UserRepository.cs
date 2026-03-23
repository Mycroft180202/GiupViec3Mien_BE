using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Phone == phone);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.WorkerProfile)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetAllWorkersAsync()
    {
        return await _context.Users
            .Include(u => u.WorkerProfile)
            .Where(u => u.Role == Domain.Enums.Role.Worker)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<User>> GetNearestWorkersAsync(double lat, double lng, int limit = 10)
    {
        return await _context.Users
            .FromSqlRaw($@"
                SELECT * FROM ""Users"" 
                WHERE ""Role"" = 2 
                ORDER BY ST_Distance(
                    ST_SetSRID(ST_MakePoint(""Longitude"", ""Latitude""), 4326)::geography, 
                    ST_SetSRID(ST_MakePoint({lng}, {lat}), 4326)::geography
                )
                LIMIT {limit}")
            .Include(u => u.WorkerProfile)
            .ToListAsync();
    }


    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
