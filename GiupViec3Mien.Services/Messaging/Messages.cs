using System;

namespace GiupViec3Mien.Services.Messaging;

// 1. Decoupling Email & Notifications
public record SendEmailMessage(string To, string Subject, string Body);

// 2. Heavy Computational Matching
public record JobPostedEvent(Guid JobId, string Title, double Latitude, double Longitude, string? RequiredSkills);

// 3. File Processing (CV Uploads)
public record ProcessCvTask(Guid ApplicationId, string TempFilePath, string FileName);

// 4. Real-time Analytics & Logging
public record AnalyticsEvent(string EventType, Guid? UserId, string Data);

// 5. Handling Transactional Bursts
public record JobApplicationTask(Guid UserId, Guid JobId, string Message, decimal BidPrice, string? CvUrl, DateTime? AvailableStartDate);

// 6. Chat Persistence & Notifications
public record MessageSentEvent(Guid SenderId, Guid ReceiverId, string Message, string RoomId);

// 7. Elasticsearch Data Synchronization
public record JobIndexMessage(
    Guid JobId, 
    string Title, 
    string Description, 
    string Category, 
    decimal Price, 
    double Lat, 
    double Lon, 
    string? RequiredSkills, 
    string Status, 
    string PostType, 
    DateTime CreatedAt,
    Guid EmployerId,
    string EmployerName,
    string? EmployerAvatarUrl,
    int ApplicantCount
);

public record JobDeleteMessage(Guid JobId);
