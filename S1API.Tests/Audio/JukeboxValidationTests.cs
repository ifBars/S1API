using S1API.Audio;

namespace S1API.Tests.Audio;

public sealed class JukeboxValidationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(26, 27)]
    public void ConfiguredTrackIndexesAreAccepted(int trackIndex, int trackCount)
    {
        Jukebox.ValidateTrackIndex(trackIndex, trackCount);
    }

    [Theory]
    [InlineData(-1, 27)]
    [InlineData(27, 27)]
    [InlineData(0, 0)]
    public void UnconfiguredTrackIndexesAreRejected(int trackIndex, int trackCount)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Jukebox.ValidateTrackIndex(trackIndex, trackCount));

        Assert.Equal("trackIndex", exception.ParamName);
        Assert.Equal(trackIndex, exception.ActualValue);
    }
}
