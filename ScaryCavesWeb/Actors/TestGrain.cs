namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.ITestGrain")]
public interface ITestGrain : IGrainWithIntegerKey
{
    [Alias("Ping")]
    Task<bool> Ping();
}

// Orleans 10 only loads stored state whose type it knows: [GenerateSerializer] puts it on the allow-list.
[GenerateSerializer]
[Alias("ScaryCavesWeb.Actors.TestState")]
public class TestState(bool success)
{
    [Id(0)] public bool Success { get; set; } = success;
    [Id(1)] public long Timestamp { get; set; }
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
