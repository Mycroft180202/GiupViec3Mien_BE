using GiupViec3Mien.Services.Messaging;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace GiupViec3Mien.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RabbitChatTestController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public RabbitChatTestController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost("test-message")]
    public async Task<IActionResult> TestMessage(string message)
    {
        try
        {
            // IDs from SeedTestChat
            Guid freelancerId = Guid.Parse("10af465d-9a1d-4ceb-abc8-91450451803a");
            Guid clientId = Guid.Parse("f70a5f84-1f09-425e-b50e-19dd60e398cc");
            string roomId = "10af465d-9a1d-4ceb-abc8-91450451803a_f70a5f84-1f09-425e-b50e-19dd60e398cc";

            // Simulate what ChatHub does now: just publish one event
            await _publishEndpoint.Publish(new MessageSentEvent(
                freelancerId, 
                clientId, 
                message ?? "Hello from RabbitMQ Test", 
                roomId
            ));

            return Ok(new { 
                status = "Success", 
                message = "MessageSentEvent published to RabbitMQ. Check console logs for ChatConsumer processing." 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = "Error", error = ex.Message });
        }
    }
}
