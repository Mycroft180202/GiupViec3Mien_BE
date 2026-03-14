using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _context;

    public ChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        await _context.ChatMessages.AddAsync(message);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesByRoomIdAsync(string roomId)
    {
        return await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.RoomId == roomId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
