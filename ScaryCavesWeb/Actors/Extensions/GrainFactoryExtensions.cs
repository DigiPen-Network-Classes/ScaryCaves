using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Actors.Extensions;

public static class GrainFactoryExtensions
{
    public static IRoomActor GetRoomActor(this IGrainFactory grainFactory, Location location)
    {
        return grainFactory.GetGrain<IRoomActor>(location.RoomId, location.ZoneName);
    }
}
