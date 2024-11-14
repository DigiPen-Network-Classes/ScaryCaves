namespace ScaryCavesWeb.Models;

public interface IMobile
{
    string Id { get; }
    Location GetCurrentLocation();
    void SetCurrentLocation(Location l);
}
