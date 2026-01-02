using Microsoft.AspNetCore.SignalR;

namespace Plafind.Hubs
{
    public class ReviewHub : Hub
    {
        // İşletme detay sayfasına bağlanan kullanıcıları grup olarak yönet
        public async Task JoinBusinessGroup(int businessId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"business-{businessId}");
        }

        public async Task LeaveBusinessGroup(int businessId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"business-{businessId}");
        }
    }
}

