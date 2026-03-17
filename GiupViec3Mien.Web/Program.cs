using System.Text;
using GiupViec3Mien.Persistence;
using GiupViec3Mien.Persistence.Repositories;
using GiupViec3Mien.Services.Auth;
using GiupViec3Mien.Services.Job;
using GiupViec3Mien.Services.FileStorage;
using GiupViec3Mien.Services.UserServices;
using GiupViec3Mien.Services.Matching;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using GiupViec3Mien.Presentation.Hubs;
using GiupViec3Mien.Services.Chat;
using GiupViec3Mien.Services.Email;
using GiupViec3Mien.Services.Messaging.Consumers;
using MassTransit;
using Microsoft.AspNetCore.OpenApi;

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

// Email Configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// RabbitMQ (MassTransit) Configuration
builder.Services.AddMassTransit(x =>
{
    // Register Consumers
    x.AddConsumer<EmailConsumer>();
    x.AddConsumer<MatchingConsumer>();
    x.AddConsumer<AnalyticsConsumer>();
    x.AddConsumer<ApplicationConsumer>();
    x.AddConsumer<ChatConsumer>();

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

var app = builder.Build();

// Automatically apply migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
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

            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    operation.Security ??= new List<OpenApiSecurityRequirement>();
                    operation.Security.Add(securityRequirement);
                }
            }
        }

        return Task.CompletedTask;
    }
}
