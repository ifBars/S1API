---
title: "Messaging System"
description: "Reference for the messaging system — MessagingManager, MSGConversation, Message, and phone communication"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Messaging System

## MessagingManager (`Il2CppScheduleOne.Messaging.MessagingManager`)
Manages all SMS/in-game message conversations.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `ActiveConversations` | `List<MSGConversation>` | Current active conversations |

### Methods
| Method | Description |
|--------|-------------|
| `SendMessage(string targetID, Message message)` | Send a message |
| `GetConversation(string partnerID)` | Get conversation with partner |
| `StartConversation(string partnerID)` | Begin new conversation |
| `HasUnreadMessages()` | Check for unread |

---

## MSGConversation (`Il2CppScheduleOne.Messaging.MSGConversation`)
A conversation between player and an NPC/entity.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `PartnerID` | `string` | Who the conversation is with |
| `Messages` | `List<Message>` | All messages in thread |
| `HasUnread` | `bool` | Unread messages flag |
| `LastMessageTime` | `GameDateTime` | When last message sent |
| `Category` | `EConversationCategory` | Conversation type |

### Methods
| Method | Description |
|--------|-------------|
| `AddMessage(Message msg)` | Add message to thread |
| `MarkAllRead()` | Clear unread flag |

---

## Message (`Il2CppScheduleOne.Messaging.Message`)
A single message in a conversation.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `Content` | `string` | Message text |
| `SenderID` | `string` | Who sent it |
| `Timestamp` | `GameDateTime` | When sent |
| `IsPlayerSender` | `bool` | Sent by player |
| `Responses` | `List<Response>` | Available responses |

---

## Response (`Il2CppScheduleOne.Messaging.Response`)
A player response option in a conversation.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | Response text |
| `Callback` | `ResponseCallback` | Triggered on selection |

---

## ResponseCallback (`Il2CppScheduleOne.Messaging.ResponseCallback`)
Delegate/callback when a response is selected.

---

## SendableMessage (`Il2CppScheduleOne.Messaging.SendableMessage`)
A message ready to be sent.

---

## IMessageEntity (`Il2CppScheduleOne.Messaging.IMessageEntity`)
Interface for entities that can send/receive messages.

### Methods
| Method | Description |
|--------|-------------|
| `OnMessageReceived(Message msg)` | Handle incoming message |
| `GetMessageID()` | Unique entity ID |

---

## EConversationCategory Enum
| Value | Description |
|-------|-------------|
| (various) | Categorizes conversation type (business, story, etc.) |

---

## Usage Patterns

### Sending an SMS
```csharp
// Via S1API
npc.SendTextMessage("Hey, check this out!", new Response[]
{
    new Response("Sure!", callback),
    new Response("Not now", null)
}, responseDelay: 1f, network: true);
```

### Checking unread messages
```csharp
if (Singleton<MessagingManager>.Instance.HasUnreadMessages())
{
    // Show notification
}
```

### Getting a conversation
```csharp
MSGConversation convo = Singleton<MessagingManager>.Instance
    .GetConversation("uncle_nelson");
if (convo != null)
{
    foreach (Message msg in convo.Messages)
    {
        MelonLogger.Msg($"{msg.SenderID}: {msg.Content}");
    }
}
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.Phone.SendMessage()` | Send a message |
| `Api.Phone.GetMessages()` | Get messages |
| `Api.Phone.OpenApp("Messages")` | Open Messages app |
