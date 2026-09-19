using S1API.Internal.Products;

namespace S1API.Tests.Products;

public sealed class ProductIconRenderRigArbiterTests : IDisposable
{
    public ProductIconRenderRigArbiterTests()
    {
        ProductIconRenderRigArbiter.ResetForTesting();
    }

    [Fact]
    public void CaptureLeasesAreGrantedOneAtATimeInQueueOrder()
    {
        ProductIconRenderRigArbiter.CaptureLease first =
            ProductIconRenderRigArbiter.Enqueue();
        ProductIconRenderRigArbiter.CaptureLease second =
            ProductIconRenderRigArbiter.Enqueue();

        Assert.False(ProductIconRenderRigArbiter.TryAcquire(second));
        Assert.True(ProductIconRenderRigArbiter.TryAcquire(first));
        Assert.False(ProductIconRenderRigArbiter.TryAcquire(second));

        ProductIconRenderRigArbiter.Release(first);

        Assert.True(ProductIconRenderRigArbiter.TryAcquire(second));
    }

    [Fact]
    public void CancellingAWaitingLeaseUnblocksTheNextCapture()
    {
        ProductIconRenderRigArbiter.CaptureLease first =
            ProductIconRenderRigArbiter.Enqueue();
        ProductIconRenderRigArbiter.CaptureLease cancelled =
            ProductIconRenderRigArbiter.Enqueue();
        ProductIconRenderRigArbiter.CaptureLease last =
            ProductIconRenderRigArbiter.Enqueue();

        Assert.True(ProductIconRenderRigArbiter.TryAcquire(first));
        ProductIconRenderRigArbiter.Cancel(cancelled);
        ProductIconRenderRigArbiter.Release(first);

        Assert.False(ProductIconRenderRigArbiter.TryAcquire(cancelled));
        Assert.True(ProductIconRenderRigArbiter.TryAcquire(last));
    }

    public void Dispose()
    {
        ProductIconRenderRigArbiter.ResetForTesting();
    }
}
