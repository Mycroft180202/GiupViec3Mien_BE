using System.Text;
using GiupViec3Mien.Persistence;
using GiupViec3Mien.Persistence.Repositories;
using GiupViec3Mien.Services.Auth;
using GiupViec3Mien.Services.Job;
using GiupViec3Mien.Services.FileStorage;
using GiupViec3Mien.Services.UserServices;
using GiupViec3Mien.Services.Matching;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Elastic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using GiupViec3Mien.Presentation.Hubs;
using GiupViec3Mien.Services.Chat;
using GiupViec3Mien.Services.Subscription;
using GiupViec3Mien.Services.Email;
using GiupViec3Mien.Services.Messaging.Consumers;
using MassTransit;
using Microsoft.AspNetCore.OpenApi;
using Hangfire;
using Hangfire.PostgreSql;
using GiupViec3Mien.Services.BackgroundJobs;
using GiupViec3Mien.Services.NewsFeed;
using GiupViec3Mien.Services.Training;
using Elastic.Clients.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddControllers()
    .AddApplicationPart(typeof(GiupViec3Mien.Presentation.Controllers.AuthController).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// Configure Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseNetTopologySuite()));

// Configure Repositories & Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IFileStorageService, CloudinaryService>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<IJobSearchService, JobSearchService>();
builder.Services.AddScoped<IWorkerSearchService, WorkerSearchService>();


// News Feed
builder.Services.AddScoped<INewsPostRepository, NewsPostRepository>();
builder.Services.AddScoped<INewsService, NewsService>();

// Training Courses
builder.Services.AddScoped<ITrainingCourseRepository, TrainingCourseRepository>();
builder.Services.AddScoped<ITrainingCourseService, TrainingCourseService>();

// Email Configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// builder.Services.AddScoped<IBackgroundJob, WeeklySummaryJob>(); // Disabled weekly summary/newsletter

// Hangfire Background Job Configuration
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(connectionString);
    })
    .UseFilter(new AutomaticRetryAttribute { Attempts = 5 })); // Automatic Retries: Global limit of 5 attempts

// Resource Control: Define specific queues for the server to process
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "high-priority", "email", "default" };
    options.WorkerCount = Environment.ProcessorCount * 2; // Performance tuning
});

// Register Background Jobs
builder.Services.AddScoped<IBackgroundJob, JobExpirationJob>();
// builder.Services.AddScoped<IBackgroundJob, NewsletterJob>(); // Disabled weekly newsletter
builder.Services.AddScoped<ProfileReminderJob>(); // For delayed scheduling
builder.Services.AddScoped<SendEmailJob>(); // For one-off reliable emails
builder.Services.AddScoped<JobMatchingJob>(); // For heavy-computational matching
builder.Services.AddScoped<ProcessCVJob>(); // For reliable file processing

// Configure Elasticsearch Client
var esSettings = builder.Configuration.GetSection("Elasticsearch");
var esUrl = esSettings["Url"] ?? "http://localhost:9200";
var esDefaultIndex = esSettings["DefaultIndex"] ?? "jobs";

var clientSettings = new ElasticsearchClientSettings(new Uri(esUrl))
    .DefaultIndex(esDefaultIndex);

builder.Services.AddSingleton(new ElasticsearchClient(clientSettings));

// RabbitMQ (MassTransit) Configuration
builder.Services.AddMassTransit(x =>
{
    // Register Consumers
    x.AddConsumer<EmailConsumer>();
    x.AddConsumer<MatchingConsumer>();
    x.AddConsumer<AnalyticsConsumer>();
    x.AddConsumer<ApplicationConsumer>();
    x.AddConsumer<ChatConsumer>();
    x.AddConsumer<JobElasticSyncConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitSettings = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(rabbitSettings["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbitSettings["Username"] ?? "guest");
            h.Password(rabbitSettings["Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "MySuperSecretKeyForGiupViec3MienApiIsHere!!123";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Allow any origin for development
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseRouting();
app.UseCors();


// Automatically apply migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
        
        // Seed initial data
        await GiupViec3Mien.Persistence.Seeders.SeedJobs.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while migrating the database: {ex.Message}");
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("GiupViec3Mien API");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

// Hangfire Dashboard & Job Registration
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

using (var scope = app.Services.CreateScope())
{
    HangfireJobRegistrar.RegisterJobs(scope.ServiceProvider);
}

 app.Run();

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };
        
        document.Components ??= new OpenApiComponents();
        if (document.Components.SecuritySchemes == null)
        {
            var type = document.Components.GetType().GetProperty("SecuritySchemes")!.PropertyType;
            var dictType = typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments());
            document.Components.SecuritySchemes = (dynamic)Activator.CreateInstance(dictType)!;
        }
        document.Components.SecuritySchemes["Bearer"] = authenticationScheme;

        if (document.Paths != null)
        {
            var securityRequirement = new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
            };

            if (document.Paths != null)
            {
                foreach (var path in document.Paths.Values)
                {
                    if (path?.Operations == null) continue;
                    foreach (var operation in path.Operations.Values)
                    {
                        operation.Security ??= new List<OpenApiSecurityRequirement>();
                        operation.Security.Add(securityRequirement);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}
