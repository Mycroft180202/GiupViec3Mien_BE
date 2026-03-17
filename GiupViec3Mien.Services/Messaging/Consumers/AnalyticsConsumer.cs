using GiupViec3Mien.Services.Messaging;
using MassTransit;
using System.Threading.Tasks;
using System;

namespace GiupViec3Mien.Services.Messaging.Consumers;

public class AnalyticsConsumer : IConsumer<AnalyticsEvent>
{
    public Task Consume(ConsumeContext<AnalyticsEvent> context)
    {
        var evt = context.Message;
        
        // Logic for Point 4: Decoupled logging/analytics
        Console.WriteLine($"[Analytics] Type: {evt.EventType}, User: {evt.UserId}, Data: {evt.Data}");
        
        // Here you would save to MongoDB, Elasticsearch, or a separate SQL DB
        return Task.CompletedTask;
    }
}
