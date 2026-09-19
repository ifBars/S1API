using S1API.Audio;

namespace S1API.Tests.Audio;

public sealed class JukeboxRepeatModeTests
{
    [Fact]
    public void ValuesMatchTheNativeRepeatModes()
    {
        Assert.Equal(0, (int)JukeboxRepeatMode.None);
        Assert.Equal(1, (int)JukeboxRepeatMode.RepeatQueue);
        Assert.Equal(2, (int)JukeboxRepeatMode.RepeatTrack);
    }
}
