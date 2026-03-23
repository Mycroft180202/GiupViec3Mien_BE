using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Seeders;

public class SeedTestChat
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Create/Update a Client (Employer)
        var client = await context.Users.FirstOrDefaultAsync(u => u.Phone == "0900000123");
        if (client == null)
        {
            client = new User
            {
                FullName = "Nguyễn Chủ Nhà",
                Phone = "0900000123",
                Email = "thanhhovn2016@yandex.com",
                PasswordHash = "hashed_pw",
                Role = Domain.Enums.Role.Employer,
                Latitude = 10.7769,
                Longitude = 106.7009
            };
            await context.Users.AddAsync(client);
        }
        else
        {
            client.Email = "thanhhovn2016@yandex.com";
        }

        // 2. Create/Update a Freelancer (Worker)
        var freelancer = await context.Users.FirstOrDefaultAsync(u => u.Phone == "0911111456");
        if (freelancer == null)
        {
            freelancer = new User
            {
                FullName = "Trần Thợ Điện",
                Phone = "0911111456",
                Email = "thanhhovn2016@gmail.com",
                PasswordHash = "hashed_pw",
                Role = Domain.Enums.Role.Worker,
                Latitude = 10.7800,
                Longitude = 106.7050
            };
            await context.Users.AddAsync(freelancer);
        }
        else
        {
            freelancer.Email = "thanhhovn2016@gmail.com";
        }

        await context.SaveChangesAsync();

        // 3. Calculate RoomId
        var ids = new List<string> { freelancer.Id.ToString(), client.Id.ToString() };
        ids.Sort();
        string roomId = string.Join("_", ids);

        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("CHAT SEED DATA GENERATED:");
        Console.WriteLine($"Freelancer ID: {freelancer.Id}");
        Console.WriteLine($"Client ID:     {client.Id}");
        Console.WriteLine($"Target RoomId: {roomId}");
        Console.WriteLine($"Test URL:      /api/Chat/room-id/{freelancer.Id}/{client.Id}");
        Console.WriteLine("--------------------------------------------------");

        // 4. Seed some messages if none exist
        if (!await context.ChatMessages.AnyAsync(m => m.RoomId == roomId))
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { 
                    SenderId = client.Id, 
                    ReceiverId = freelancer.Id, 
                    Message = "Chào bạn, bạn có rảnh vào sáng mai không?", 
                    RoomId = roomId,
                    SentAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new ChatMessage { 
                    SenderId = freelancer.Id, 
                    ReceiverId = client.Id, 
                    Message = "Chào anh, tôi rảnh ạ. Mấy giờ tôi có thể qua được?", 
                    RoomId = roomId,
                    SentAt = DateTime.UtcNow.AddMinutes(-8)
                },
                new ChatMessage { 
                    SenderId = client.Id, 
                    ReceiverId = freelancer.Id, 
                    Message = "Tầm 9h sáng nhé bạn.", 
                    RoomId = roomId,
                    SentAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };
            await context.ChatMessages.AddRangeAsync(messages);
            await context.SaveChangesAsync();
            Console.WriteLine("Initial messages seeded.");
        }
    }
}
