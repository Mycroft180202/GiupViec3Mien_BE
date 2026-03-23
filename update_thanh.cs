using GiupViec3Mien.Persistence;
using Microsoft.EntityFrameworkCore;
using GiupViec3Mien.Domain.Entities;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseNpgsql("Host=localhost;Database=GiupViec3Mien;Username=postgres;Password=postgres");

using (var context = new ApplicationDbContext(optionsBuilder.Options))
{
    var userId = Guid.Parse("0d62431d-54cb-4e41-9e9d-5e6542b4976e");
    var profile = await context.WorkerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
    
    if (profile != null)
    {
        profile.HourlyRate = 60000;
        profile.Bio = "Hello, I am Ho Cong Thanh dev. I provide expert cleaning services.";
        profile.Skills = "[\"Giúp việc nhà\", \"Nấu ăn\"]";
        await context.SaveChangesAsync();
        Console.WriteLine("Worker profile updated successfully.");
    }
    else
    {
        Console.WriteLine("Worker profile not found.");
    }
}
