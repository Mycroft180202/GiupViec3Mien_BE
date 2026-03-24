using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GiupViec3Mien.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var webProjectPath = Path.GetFullPath(Path.Combine(basePath, "..", "GiupViec3Mien.Web"));
        var appsettingsPath = Path.Combine(webProjectPath, "appsettings.json");
        if (!File.Exists(appsettingsPath))
        {
            throw new InvalidOperationException("Could not find appsettings.json for design-time DbContext creation.");
        }

        using var stream = File.OpenRead(appsettingsPath);
        using var document = JsonDocument.Parse(stream);

        var connectionString = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection was not found.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
