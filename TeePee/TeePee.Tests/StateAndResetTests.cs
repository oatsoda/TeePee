namespace TeePee.Tests;

/// <summary>
/// Tests to ensure that re-use and Reset scenarios behave correctly.
/// </summary>
public class StateAndResetTests
{
    private readonly HttpRequestMessage m_MatchingHttpRequest;
    private readonly Tracker m_MatchingTracker;

    private readonly TeePeeBuilder m_Builder;

    public StateAndResetTests()
    {
        m_MatchingHttpRequest = new(HttpMethod.Get, "http://unit.test/call");

        m_Builder = new TeePeeBuilder();
        m_MatchingTracker = m_Builder
            .ForRequest(m_MatchingHttpRequest.RequestUri!.ToString(), m_MatchingHttpRequest.Method)
            .TrackRequest();
    }

    [Fact]
    public async Task RetainsState_AcrossMultipleUsesOfBuilder_()
    {
        // Given
        await m_Builder.Manual().CreateClient().GetAsync("http://unit.test/call", TestCt);

        // When
        await m_Builder.Manual().CreateClient().GetAsync("http://unit.test/call", TestCt);

        // Then
        m_MatchingTracker.WasCalled(2);
    }

    [Fact]
    public async Task Reset_ClearsState_AcrossMultipleUsesOfBuilder()
    {
        // Given
        await m_Builder.Manual().CreateClient().GetAsync("http://unit.test/call", TestCt);
        m_Builder.Reset();

        // When
        await m_Builder.Manual().CreateClient().GetAsync("http://unit.test/call", TestCt);

        // Then
        m_MatchingTracker.WasCalled(1);
    }

    [Fact]
    public async Task Reset_ClearsState_ForSameUseOfBuilder()
    {
        // Given
        var client = m_Builder.Manual().CreateClient();
        await client.GetAsync("http://unit.test/call", TestCt);
        m_Builder.Reset();

        // When
        await client.GetAsync("http://unit.test/call", TestCt);

        // Then
        m_MatchingTracker.WasCalled(1);
    }
}
