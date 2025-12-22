using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace VivuqeQRSystem.Hubs
{
    public class AttendanceHub : Hub
    {
        public async Task JoinEventGroup(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, eventId);
            }
        }

        public async Task SendAttendanceUpdate(int totalAttendance, int eventId, string guestName, string timeAgo, string eventName)
        {
            // Fallback for simple calls, but controller should use Clients.Group
            await Clients.Group(eventId.ToString()).SendAsync("ReceiveAttendanceUpdate", totalAttendance, eventId, guestName, timeAgo, eventName);
        }
    }
}
