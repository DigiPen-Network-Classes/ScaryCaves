namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.ITestGrain")]
public interface ITestGrain : IGrainWithIntegerKey
{
    [Alias("Ping")]
    Task<bool> Ping();
}

public class TestState(bool success)
{
    public bool Success { get; set; } = success;
    public long Timestamp { get; set; }
}

public class TestGrain([PersistentState(nameof(TestState))] IPersistentState<TestState> testState): Grain, ITestGrain
{
    private IPersistentState<TestState> TestState { get; } = testState;

    public async Task<bool> Ping()
    {
        if (!TestState.RecordExists)
        {
            TestState.State = new TestState(true);
        }
        TestState.State.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await TestState.WriteStateAsync();
        return true;
    }
}
