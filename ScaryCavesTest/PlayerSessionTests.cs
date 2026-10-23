using ScaryCavesWeb.Models;

namespace ScaryCavesTest;

public class PlayerSessionTests
{
    private static Player WithConnection(string? id) => new(Guid.NewGuid(), "Bob", 0, "scary-cave", id);

    [Fact] public void OwnsSession_SameId_True() => Assert.True(WithConnection("s1").OwnsSession("s1"));
    [Fact] public void OwnsSession_OtherId_False() => Assert.False(WithConnection("s2").OwnsSession("s1"));
    [Fact] public void OwnsSession_Null_AlwaysTrue() => Assert.True(WithConnection("s2").OwnsSession(null));
    [Fact] public void OwnsSession_NoCurrentSession_False() => Assert.False(WithConnection(null).OwnsSession("s1"));
}
