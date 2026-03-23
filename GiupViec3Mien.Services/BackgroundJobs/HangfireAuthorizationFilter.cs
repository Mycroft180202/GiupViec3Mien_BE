using Hangfire.Dashboard;

namespace GiupViec3Mien.Services.BackgroundJobs;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // For now, allow everyone to access dashboard.
        // In production, check claims or IP address.
        // var httpContext = context.GetHttpContext();
        // return httpContext.User.Identity?.IsAuthenticated ?? false;
        return true; 
    }
}
