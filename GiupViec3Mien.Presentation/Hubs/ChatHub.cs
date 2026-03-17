using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Messaging;
using MassTransit;

namespace GiupViec3Mien.Presentation.Hubs;

public class ChatHub : Hub
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ChatHub(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
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

        // 1. Publish Event (DB Saving + Email Notifications handled by ChatConsumer)
        await _publishEndpoint.Publish(new MessageSentEvent(senderId, receiverId, message, roomId));

        // 2. Broadcast to group (Instant UI feedback)
        await Clients.Group(roomId).SendAsync("ReceiveMessage", senderId.ToString(), message, DateTime.UtcNow);
    }
}
