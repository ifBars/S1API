using System;
using S1API.Internal.Rendering;

namespace S1API.Tests.Rendering;

public sealed class MugshotCacheTests
{
    [Fact]
    public void KeyIsStableForTheSameInputs()
    {
        Assert.Equal(
            MugshotCache.BuildKey("env", "scene", "{\"Height\":1.8}", "{}", "tex"),
            MugshotCache.BuildKey("env", "scene", "{\"Height\":1.8}", "{}", "tex"));
    }

    [Fact]
    public void KeyIsAFileNameSafeSha256()
    {
        var key = MugshotCache.BuildKey("env", "scene", "a", "b", "c");

        Assert.Equal(64, key.Length);
        Assert.Matches("^[0-9a-f]{64}$", key);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void KeyChangesWhenAnyPartChanges(int changedPart)
    {
        var parts = new[] { "env", "scene", "{\"SkinColor\":1}", "{\"AvatarObjects\":[]}", "hair=tex@512x512" };
        var original = MugshotCache.BuildKey(parts);

        parts[changedPart] += "x";

        Assert.NotEqual(original, MugshotCache.BuildKey(parts));
    }

    [Fact]
    public void KeySeparatesPartBoundaries()
    {
        Assert.NotEqual(
            MugshotCache.BuildKey("ab", "c"),
            MugshotCache.BuildKey("a", "bc"));
    }

    [Fact]
    public void KeyTreatsNullPartAsEmpty()
    {
        Assert.Equal(
            MugshotCache.BuildKey("env", null!),
            MugshotCache.BuildKey("env", string.Empty));
    }

    [Fact]
    public void EmptyFileIsRejected()
    {
        Assert.Equal("file is empty", MugshotCache.CheckFileBytes(Array.Empty<byte>()));
        Assert.Equal("file is empty", MugshotCache.CheckFileBytes(null));
    }

    [Fact]
    public void TruncatedFileIsRejected()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 };

        Assert.StartsWith("file is truncated", MugshotCache.CheckFileBytes(bytes));
    }

    [Fact]
    public void FileWithoutPngSignatureIsRejected()
    {
        var bytes = new byte[64];
        bytes[0] = (byte)'{';

        Assert.Equal("file is not a PNG", MugshotCache.CheckFileBytes(bytes));
    }

    [Fact]
    public void FileWithPngSignatureIsPassedToTheDecoder()
    {
        var bytes = new byte[64];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(bytes, 0);

        Assert.Null(MugshotCache.CheckFileBytes(bytes));
    }

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, false)]
    [InlineData(31, true)]
    public void PortraitsExpireAfterThirtyUnusedDays(int daysUnused, bool expected)
    {
        var now = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(expected, MugshotCache.IsExpired(now.AddDays(-daysUnused), now));
    }
}
