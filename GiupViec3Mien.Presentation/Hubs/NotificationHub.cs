using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GiupViec3Mien.Presentation.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
