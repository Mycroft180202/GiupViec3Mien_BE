using GiupViec3Mien.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IChatRepository
{
    Task AddMessageAsync(ChatMessage message);
    Task<IEnumerable<ChatMessage>> GetMessagesByRoomIdAsync(string roomId);
    Task SaveChangesAsync();
}
