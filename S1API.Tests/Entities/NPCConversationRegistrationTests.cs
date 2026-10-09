#if MONOMELON
using System.Runtime.CompilerServices;
using S1API.Internal.Entities;
using S1API.Internal.Utils;
using ScheduleOne.Messaging;

namespace S1API.Tests.Entities;

public sealed class NPCConversationRegistrationTests
{
    [Fact]
    public void SpawnMovesTheSameConversationAndRemovesItsPlaceholderRegistration()
    {
        var conversation = CreateConversation("messageconversation_65535");
        var registry = new Dictionary<string, MSGConversation> { [conversation.ConversationId] = conversation };

        NPCConversationLifecycle.RebindConversation(conversation, registry, "messageconversation_42");

        Assert.Single(registry);
        Assert.Same(conversation, registry["messageconversation_42"]);
        Assert.Equal("messageconversation_42", conversation.ConversationId);
    }

    [Fact]
    public void MovingOneContactDoesNotRemoveAnotherContactsSharedPlaceholder()
    {
        var conversation = CreateConversation("messageconversation_65535");
        var other = CreateConversation(conversation.ConversationId);
        var registry = new Dictionary<string, MSGConversation> { [other.ConversationId] = other };

        NPCConversationLifecycle.RebindConversation(conversation, registry, "messageconversation_42");

        Assert.Equal(2, registry.Count);
        Assert.Same(other, registry["messageconversation_65535"]);
        Assert.Same(conversation, registry["messageconversation_42"]);
    }

    [Fact]
    public void RepeatedFinalizationDoesNotReplaceOrDuplicateTheConversation()
    {
        var conversation = CreateConversation("messageconversation_65535");
        var registry = new Dictionary<string, MSGConversation>();

        NPCConversationLifecycle.RebindConversation(conversation, registry, "messageconversation_42");
        NPCConversationLifecycle.RebindConversation(conversation, registry, "messageconversation_42");

        Assert.Single(registry);
        Assert.Same(conversation, registry["messageconversation_42"]);
    }

    [Fact]
    public void MatchingIdRestoresMissingRegistration()
    {
        var conversation = CreateConversation("messageconversation_42");
        var registry = new Dictionary<string, MSGConversation>();

        NPCConversationLifecycle.RebindConversation(conversation, registry, conversation.ConversationId);

        Assert.Same(conversation, Assert.Single(registry).Value);
    }

    [Fact]
    public void MatchingIdStillRejectsAnotherConversationsRegistration()
    {
        var conversation = CreateConversation("messageconversation_42");
        var other = CreateConversation(conversation.ConversationId);
        var registry = new Dictionary<string, MSGConversation> { [other.ConversationId] = other };

        Assert.Throws<InvalidOperationException>(() =>
            NPCConversationLifecycle.RebindConversation(conversation, registry, conversation.ConversationId));

        Assert.Same(other, Assert.Single(registry).Value);
    }

    [Fact]
    public void ConflictingSpawnRegistrationLeavesBothConversationsUntouched()
    {
        var conversation = CreateConversation("messageconversation_65535");
        var other = CreateConversation("messageconversation_42");
        var registry = new Dictionary<string, MSGConversation>
        {
            [conversation.ConversationId] = conversation,
            [other.ConversationId] = other
        };

        Assert.Throws<InvalidOperationException>(() =>
            NPCConversationLifecycle.RebindConversation(conversation, registry, other.ConversationId));

        Assert.Equal("messageconversation_65535", conversation.ConversationId);
        Assert.Same(conversation, registry[conversation.ConversationId]);
        Assert.Same(other, registry[other.ConversationId]);
    }

    private static MSGConversation CreateConversation(string id)
    {
        // The Mono conversation is a managed class. Avoid its constructor, which registers
        // with Unity singletons; no native wrappers or shared registries are created here.
        var conversation = (MSGConversation)RuntimeHelpers.GetUninitializedObject(typeof(MSGConversation));
        Assert.True(ReflectionUtils.TrySetFieldOrProperty(conversation, nameof(conversation.ConversationId), id));
        return conversation;
    }
}
#endif
