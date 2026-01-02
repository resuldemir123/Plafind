using Microsoft.AspNetCore.SignalR;

namespace Plafind.Hubs
{
    public class ReservationHub : Hub
    {
        // İşletme sahibini rezervasyon bildirimleri için gruba ekle
        public async Task JoinBusinessOwnerGroup(string ownerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"business-owner-{ownerId}");
        }

        // İşletme sahibini gruptan çıkar
        public async Task LeaveBusinessOwnerGroup(string ownerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"business-owner-{ownerId}");
        }
    }
}

