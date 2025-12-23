namespace Plafind.Features.Businesses.Services
{
    public interface ILocationService
    {
        Task<(double Latitude, double Longitude)?> GetUserLocationAsync();
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
        string FormatDistance(double distanceInKm);
    }
}

