using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebApplication1.Hubs
{
    public class BookingHub : Hub
    {
        public async Task NotifyBookingStatusChange()
        {
            await Clients.All.SendAsync("RefreshBookingStatus");
        }
        
        public async Task NotifyUpcomingBooking()
        {
            await Clients.All.SendAsync("UpcomingBookingAlert");
        }
    }
} 