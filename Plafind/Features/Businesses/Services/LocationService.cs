namespace Plafind.Features.Businesses.Services
{
    public class LocationService : ILocationService
    {
        public async Task<(double Latitude, double Longitude)?> GetUserLocationAsync()
        {
            // Bu metod client-side JavaScript ile çağrılacak
            // Şimdilik null döndürüyoruz, JavaScript tarafında implement edilecek
            await Task.CompletedTask;
            return null;
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        public string FormatDistance(double distanceInKm)
        {
            if (distanceInKm < 1)
            {
                return $"{(int)(distanceInKm * 1000)} m";
            }
            return $"{distanceInKm:F1} km";
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}

