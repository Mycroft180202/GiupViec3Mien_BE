using GiupViec3Mien.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IChatService
{
    Task<ChatMessage> SaveMessageAsync(Guid senderId, Guid receiverId, string message, string roomId);
    Task<IEnumerable<ChatMessage>> GetChatHistoryAsync(string roomId);
}
