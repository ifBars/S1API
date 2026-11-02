using S1API.Internal.Utils;

namespace S1API.Tests.Internal.Utils;

public sealed class BatchedPassTests
{
    [Fact]
    public void RunsAtOnceOutsideABatch()
    {
        int runs = 0;
        var pass = new BatchedPass(() => runs++);
        pass.Request();
        pass.Request();
        Assert.Equal(2, runs);
    }

    [Fact]
    public void RunsOnceAtTheEndOfABatch()
    {
        int runs = 0;
        var pass = new BatchedPass(() => runs++);
        pass.Begin();
        pass.Request();
        pass.Request();
        Assert.Equal(0, runs);
        pass.End();
        Assert.Equal(1, runs);
    }

    [Fact]
    public void ABatchWithoutARequestDoesNotRun()
    {
        int runs = 0;
        var pass = new BatchedPass(() => runs++);
        pass.Begin();
        pass.End();
        Assert.Equal(0, runs);
    }

    [Fact]
    public void NestedBatchesRunAfterTheOutermostEnd()
    {
        int runs = 0;
        var pass = new BatchedPass(() => runs++);
        pass.Begin();
        pass.Begin();
        pass.Request();
        pass.End();
        Assert.Equal(0, runs);
        pass.End();
        Assert.Equal(1, runs);
    }

    [Fact]
    public void AnUnmatchedEndDoesNothing()
    {
        int runs = 0;
        var pass = new BatchedPass(() => runs++);
        pass.End();
        pass.Request();
        Assert.Equal(1, runs);
    }
}
