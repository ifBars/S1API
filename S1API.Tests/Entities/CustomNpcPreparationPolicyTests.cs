using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class CustomNpcPreparationPolicyTests
{
    [Fact]
    public void PreparedInstanceIsReusedOnlyForItsExactCustomType()
    {
        var first = new FirstCustomNpc();
        var second = new SecondCustomNpc();
        object[] instances = { first, second };

        object? result = CustomNpcPreparationPolicy.FindExactType(
            instances,
            typeof(SecondCustomNpc));

        Assert.Same(second, result);
    }

    [Fact]
    public void MissingPreparedTypeRequiresNewConstruction()
    {
        object[] instances = { new FirstCustomNpc() };

        object? result = CustomNpcPreparationPolicy.FindExactType(
            instances,
            typeof(SecondCustomNpc));

        Assert.Null(result);
    }

    private sealed class FirstCustomNpc
    {
    }

    private sealed class SecondCustomNpc
    {
    }
}
