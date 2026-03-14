using GiupViec3Mien.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GiupViec3Mien.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("history/{roomId}")]
    public async Task<IActionResult> GetChatHistory(string roomId)
    {
        var history = await _chatService.GetChatHistoryAsync(roomId);
        
        var response = history.Select(m => new {
            m.Id,
            m.SenderId,
            m.ReceiverId,
            m.Message,
            m.SentAt,
            m.RoomId
        });

        return Ok(response);
    }
    
    [HttpGet("room-id/{freelancerId}/{clientId}")]
    public IActionResult GetRoomId(string freelancerId, string clientId)
    {
        var ids = new List<string> { freelancerId, clientId };
        ids.Sort();
        string roomId = string.Join("_", ids);
        return Ok(new { roomId });
    }
}
