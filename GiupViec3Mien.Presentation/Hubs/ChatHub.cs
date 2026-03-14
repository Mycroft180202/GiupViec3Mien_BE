using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GiupViec3Mien.Services.Interfaces;

namespace GiupViec3Mien.Presentation.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public ChatHub(IChatService chatService, IUserRepository userRepository, IEmailService emailService)
    {
        _chatService = chatService;
        _userRepository = userRepository;
        _emailService = emailService;
    }

    // Simple in-memory tracker of which user is connected with which ConnectionId
    private static readonly ConcurrentDictionary<string, string> UserConnections = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            UserConnections[userId] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            UserConnections.TryRemove(userId, out _);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string freelancerId, string clientId)
    {
        // Sort IDs to ensure RoomId is consistent regardless of who joins first
        var ids = new List<string> { freelancerId, clientId };
        ids.Sort();
        string roomId = string.Join("_", ids);

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        
        // Notify others in room
        await Clients.Group(roomId).SendAsync("UserJoined", Context.User?.Identity?.Name ?? "User", roomId);
    }

    public async Task SendMessageToRoom(string roomId, string message)
    {
        var senderIdString = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (senderIdString == null || !Guid.TryParse(senderIdString, out var senderId)) return;

        // Extract receiverId from roomId (clientId_freelancerId)
        var ids = roomId.Split('_');
        if (ids.Length != 2) return;

        var receiverIdString = ids[0] == senderIdString ? ids[1] : ids[0];
        if (!Guid.TryParse(receiverIdString, out var receiverId)) return;

        // Save to DB
        await _chatService.SaveMessageAsync(senderId, receiverId, message, roomId);

        // Notify via Email
        var receiver = await _userRepository.GetByIdAsync(receiverId);
        var sender = await _userRepository.GetByIdAsync(senderId);
        
        if (receiver != null && !string.IsNullOrEmpty(receiver.Email))
        {
            try 
            {
                string subject = $"New message from {sender?.FullName ?? "User"}";
                string body = $@"
                    <h2>New Chat Message</h2>
                    <p><strong>{sender?.FullName ?? "Someone"}</strong> sent you a message:</p>
                    <blockquote style='padding: 10px; background: #f9f9f9; border-left: 5px solid #ccc;'>
                        {message}
                    </blockquote>
                    <p>Go to the app to reply!</p>";
                
                // Fire and forget email or await? Better await for consistency in test
                await _emailService.SendEmailAsync(receiver.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send notification email: {ex.Message}");
            }
        }

        // Broadcast to group
        await Clients.Group(roomId).SendAsync("ReceiveMessage", senderId.ToString(), message, DateTime.UtcNow);
    }
}
