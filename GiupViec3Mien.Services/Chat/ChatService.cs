using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Chat;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;

    public ChatService(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<ChatMessage> SaveMessageAsync(Guid senderId, Guid receiverId, string message, string roomId)
    {
        var chatMessage = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Message = message,
            RoomId = roomId,
            SentAt = DateTime.UtcNow
        };

        await _chatRepository.AddMessageAsync(chatMessage);
        await _chatRepository.SaveChangesAsync();

        return chatMessage;
    }

    public async Task<IEnumerable<ChatMessage>> GetChatHistoryAsync(string roomId)
    {
        return await _chatRepository.GetMessagesByRoomIdAsync(roomId);
    }
}
