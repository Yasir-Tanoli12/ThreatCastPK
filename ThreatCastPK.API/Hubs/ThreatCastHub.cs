using Microsoft.AspNetCore.SignalR;

namespace ThreatCastPK.API.Hubs
{
    public class ThreatCastHub : Hub
    {
<<<<<<< HEAD
=======
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

>>>>>>> haadi-cyber
        public async Task JoinCityGroup(string city)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"city_{city}");
        }

        public async Task LeaveCityGroup(string city)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"city_{city}");
        }

        public async Task JoinSectorGroup(string sector)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"sector_{sector}");
        }

        public async Task LeaveSectorGroup(string sector)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sector_{sector}");
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_viewers");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_viewers");
            await base.OnDisconnectedAsync(exception);
        }
    }
}