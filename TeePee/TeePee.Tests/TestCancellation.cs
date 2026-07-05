namespace TeePee.Tests;

public static class TestCancellation
{
    public static CancellationToken TestCt => TestContext.Current.CancellationToken;
}