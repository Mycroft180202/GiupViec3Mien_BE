using System;

namespace GiupViec3Mien.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public User? Sender { get; set; }
    
    public Guid ReceiverId { get; set; }
    public User? Receiver { get; set; }
    
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    // Convention: clientId_freelancerId (sorted alphabetically to be consistent)
    public string RoomId { get; set; } = string.Empty;
}
