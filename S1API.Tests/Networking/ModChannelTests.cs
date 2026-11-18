using S1API.Internal.Networking;
using S1API.Networking;

namespace S1API.Tests.Networking;

public sealed class ModChannelTests : IDisposable
{
    private readonly List<string> _log = new();

    public ModChannelTests()
    {
        ModChannel.ResetForTesting();
        ModChannel.LogSinkForTesting = _log.Add;
    }

    public void Dispose()
    {
        ModChannel.ResetForTesting();
        ModChannel.LogSinkForTesting = null;
    }

    [Fact]
    public void GetReturnsOneChannelPerId()
    {
        Assert.Same(ModChannel.Get("doodesch.chassis"), ModChannel.Get("doodesch.chassis"));
        Assert.NotSame(ModChannel.Get("doodesch.chassis"), ModChannel.Get("doodesch.rollin"));
        Assert.Equal("doodesch.rollin", ModChannel.Get("doodesch.rollin").Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a|b")]
    public void GetRejectsEmptyIdsAndTheSeparator(string id)
    {
        Assert.Throws<ArgumentException>(() => ModChannel.Get(id));
    }

    [Fact]
    public void FrameRoundTripKeepsSeparatorsInThePayload()
    {
        string encoded = ModChannelFrame.Encode(ModChannelFrame.ToServer, "mod.a", "x|y||z");

        Assert.True(ModChannelFrame.TryDecode(encoded, out ModChannelFrame frame));
        Assert.True(frame.IsToServer);
        Assert.Equal("mod.a", frame.ChannelId);
        Assert.Equal("x|y||z", frame.Payload);
    }

    [Fact]
    public void EmptyPayloadRoundTrips()
    {
        Assert.True(ModChannelFrame.TryDecode(ModChannelFrame.Encode(ModChannelFrame.ToClients, "mod.a", ""), out ModChannelFrame frame));
        Assert.False(frame.IsToServer);
        Assert.Equal(string.Empty, frame.Payload);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("3f2504e0-4f89-11d3-9a0c-0305e82c3301")]
    [InlineData("CHSS|req.pedal|1")]
    [InlineData("s1api|")]
    [InlineData("s1api|x|mod|p")]
    [InlineData("s1api|s||p")]
    [InlineData("s1api|smod|p")]
    public void QuestGuidsAndBrokenFramesAreNotFrames(string? value)
    {
        Assert.False(ModChannelFrame.TryDecode(value, out _));
    }

    [Fact]
    public void AThrowingHandlerDoesNotStopTheOthers()
    {
        ModChannel channel = ModChannel.Get("mod.a");
        var got = new List<string>();
        channel.OnClientReceive += _ => throw new InvalidOperationException("boom");
        channel.OnClientReceive += payload => got.Add(payload);
        channel.OnServerReceive += (_, _) => throw new InvalidOperationException("boom");
        channel.OnServerReceive += (sender, payload) => got.Add("server:" + payload + ":" + (sender == null));

        channel.DeliverToClient("hello");
        channel.DeliverToServer(null, "hi");

        Assert.Equal(new[] { "hello", "server:hi:True" }, got);
        Assert.Contains(_log, line => line.StartsWith("error: Channel 'mod.a': OnClientReceive handler of S1API.Tests threw"));
        Assert.Contains(_log, line => line.StartsWith("error: Channel 'mod.a': OnServerReceive handler of S1API.Tests threw"));
    }

    [Fact]
    public void ChannelsDoNotSeeEachOthersMessages()
    {
        var a = new List<string>();
        var b = new List<string>();
        ModChannel.Get("mod.a").OnClientReceive += a.Add;
        ModChannel.Get("mod.b").OnClientReceive += b.Add;

        Assert.True(ModChannel.TryFind("mod.a", out ModChannel? found));
        found!.DeliverToClient("for a");

        Assert.Equal(new[] { "for a" }, a);
        Assert.Empty(b);
    }

    [Fact]
    public void UnknownChannelIsReportedOnce()
    {
        Assert.False(ModChannel.TryFind("mod.missing", out _));
        Assert.False(ModChannel.TryFind("mod.missing", out _));

        Assert.Single(_log, line => line.StartsWith("warn: Channel 'mod.missing' received a message, but no mod here uses it."));
    }

    [Fact]
    public void MissingHandlerIsReportedOncePerDirection()
    {
        ModChannel channel = ModChannel.Get("mod.quiet");
        channel.DeliverToClient("1");
        channel.DeliverToClient("2");
        channel.DeliverToServer(null, "3");

        Assert.Single(_log, line => line.Contains("nothing handles OnClientReceive"));
        Assert.Single(_log, line => line.Contains("nothing handles OnServerReceive"));
    }

    [Fact]
    public void PayloadOverTheLimitIsRejectedWithAnError()
    {
        ModChannel channel = ModChannel.Get("mod.big");

        Assert.True(channel.TryFrame(ModChannelFrame.ToServer, new string('x', ModChannel.MaxPayloadChars), "SendToServer", out string frame, out _));
        Assert.True(ModChannelFrame.TryDecode(frame, out ModChannelFrame decoded));
        Assert.Equal(ModChannel.MaxPayloadChars, decoded.Payload.Length);

        Assert.False(channel.TryFrame(ModChannelFrame.ToServer, new string('x', ModChannel.MaxPayloadChars + 1), "SendToServer", out _, out _));
        Assert.Contains(_log, line => line.StartsWith("error: Channel 'mod.big': SendToServer payload has"));
    }

    [Fact]
    public void NullPayloadIsSentAsEmpty()
    {
        Assert.True(ModChannel.Get("mod.a").TryFrame(ModChannelFrame.ToClients, null, "SendToClients", out string frame, out string text));
        Assert.Equal(string.Empty, text);
        Assert.Equal("s1api|c|mod.a|", frame);
    }
}
