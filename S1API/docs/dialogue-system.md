# Dialogue System

The dialogue system allows you to create interactive conversations with branching dialogue trees, choice-based interactions, and dynamic responses.

## Table of Contents

1. [Overview](#overview)
2. [Basic Dialogue Setup](#basic-dialogue-setup)
3. [Dialogue Database](#dialogue-database)
4. [Dialogue Containers](#dialogue-containers)
5. [Choice Callbacks](#choice-callbacks)
6. [Dynamic Navigation](#dynamic-navigation)
7. [Dialogue Events](#dialogue-events)
8. [Advanced Features](#advanced-features)
9. [Best Practices](#best-practices)

## Overview

The dialogue system uses a modular approach with databases and containers:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Build dialogue database
    Dialogue.BuildAndSetDatabase(db => {
        db.WithModuleEntry("Reactions", "GREETING", "Hello there!");
        db.WithModuleEntry("Reactions", "ANGRY", "I'm not happy about this!");
    });
    
    // Create dialogue container
    Dialogue.BuildAndRegisterContainer("ShopDialogue", c => {
        c.AddNode("ENTRY", "Welcome to my shop! What can I help you with?", ch => {
            ch.Add("BUY_ITEM", "I'd like to buy something", "ITEM_SELECTION")
              .Add("SELL_ITEM", "I want to sell something", "SELL_DIALOGUE")
              .Add("LEAVE", "Never mind", "EXIT");
        });
        
        c.AddNode("ITEM_SELECTION", "Here's what I have available...", ch => {
            ch.Add("PURCHASE", "I'll take it", "PURCHASE_CONFIRM")
              .Add("BACK", "Let me think", "ENTRY");
        });
        
        c.AddNode("PURCHASE_CONFIRM", "That'll be $100. Deal?", ch => {
            ch.Add("YES", "Yes, deal!", "PURCHASE_COMPLETE")
              .Add("NO", "Too expensive", "ENTRY");
        });
        
        c.AddNode("PURCHASE_COMPLETE", "Pleasure doing business!");
        c.AddNode("EXIT", "Come back anytime!");
    });
    
    // Set up choice callbacks
    Dialogue.OnChoiceSelected("PURCHASE", () => {
        // Handle purchase logic
        var playerCash = Money.GetCashBalance();
        if (playerCash >= 100f) {
            Money.ChangeCashBalance(-100f, visualizeChange: true);
            Dialogue.JumpTo("ShopDialogue", "PURCHASE_COMPLETE");
        } else {
            Dialogue.JumpTo("ShopDialogue", "NOT_ENOUGH_CASH");
        }
    });
    
    // Use container when player interacts
    Dialogue.UseContainerOnInteract("ShopDialogue");
}
```

## Basic Dialogue Setup

### Setting Up Dialogue

Dialogue configuration is done in the `OnCreated` method:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Basic dialogue setup
    Dialogue.BuildAndSetDatabase(db => {
        db.WithModuleEntry("Reactions", "GREETING", "Hello!");
    });
    
    Dialogue.BuildAndRegisterContainer("BasicDialogue", c => {
        c.AddNode("ENTRY", "Hello there!", ch => {
            ch.Add("GREET", "Hello!", "RESPONSE");
        });
        c.AddNode("RESPONSE", "Nice to meet you!");
    });
    
    Dialogue.UseContainerOnInteract("BasicDialogue");
}
```

## Dialogue Database

### Building a Database

The dialogue database stores modular dialogue entries:

```csharp
Dialogue.BuildAndSetDatabase(db => {
    // Reaction entries
    db.WithModuleEntry("Reactions", "GREETING", "Hello there!");
    db.WithModuleEntry("Reactions", "ANGRY", "I'm not happy about this!");
    db.WithModuleEntry("Reactions", "HAPPY", "That's great news!");
    
    // Question entries
    db.WithModuleEntry("Questions", "HOW_ARE_YOU", "How are you doing?");
    db.WithModuleEntry("Questions", "WHAT_DO_YOU_DO", "What do you do for work?");
    
    // Response entries
    db.WithModuleEntry("Responses", "GOOD", "I'm doing well, thanks!");
    db.WithModuleEntry("Responses", "BAD", "Not so good, actually.");
});
```

### Database Features

- **Modular Design**: Separate dialogue modules for different contexts
- **Reusable Entries**: Use the same entries across multiple containers
- **Easy Updates**: Change dialogue text in one place
- **Localization Support**: Easy to translate and maintain

## Dialogue Containers

### Creating Containers

Containers define the flow of a conversation:

```csharp
Dialogue.BuildAndRegisterContainer("ShopDialogue", c => {
    // Entry node
    c.AddNode("ENTRY", "Welcome to my shop! What can I help you with?", ch => {
        ch.Add("BUY_ITEM", "I'd like to buy something", "ITEM_SELECTION")
          .Add("SELL_ITEM", "I want to sell something", "SELL_DIALOGUE")
          .Add("LEAVE", "Never mind", "EXIT");
    });
    
    // Item selection node
    c.AddNode("ITEM_SELECTION", "Here's what I have available...", ch => {
        ch.Add("ITEM_1", "Buy Item 1 ($50)", "PURCHASE_1")
          .Add("ITEM_2", "Buy Item 2 ($100)", "PURCHASE_2")
          .Add("BACK", "Let me think", "ENTRY");
    });
    
    // Purchase nodes
    c.AddNode("PURCHASE_1", "That'll be $50. Deal?", ch => {
        ch.Add("YES_1", "Yes, deal!", "PURCHASE_COMPLETE")
          .Add("NO_1", "Too expensive", "ITEM_SELECTION");
    });
    
    c.AddNode("PURCHASE_2", "That'll be $100. Deal?", ch => {
        ch.Add("YES_2", "Yes, deal!", "PURCHASE_COMPLETE")
          .Add("NO_2", "Too expensive", "ITEM_SELECTION");
    });
    
    // Completion nodes
    c.AddNode("PURCHASE_COMPLETE", "Pleasure doing business!");
    c.AddNode("EXIT", "Come back anytime!");
});
```

### Container Structure

- **Nodes**: Individual dialogue points with text and choices
- **Choices**: Player options that lead to other nodes
- **Flow**: Linear or branching conversation paths
- **Names**: Unique identifiers for nodes and choices

### Node Types

**Entry Node**: Starting point of the conversation
```csharp
c.AddNode("ENTRY", "Welcome! How can I help you?", ch => {
    ch.Add("OPTION_1", "I need help", "HELP_NODE")
      .Add("OPTION_2", "Just browsing", "BROWSE_NODE");
});
```

**Choice Node**: Node with player choices
```csharp
c.AddNode("HELP_NODE", "What kind of help do you need?", ch => {
    ch.Add("HELP_1", "I need information", "INFO_NODE")
      .Add("HELP_2", "I need items", "ITEMS_NODE")
      .Add("BACK", "Never mind", "ENTRY");
});
```

**End Node**: Terminal node without choices
```csharp
c.AddNode("INFO_NODE", "Here's the information you requested.");
c.AddNode("EXIT", "Thanks for your time!");
```

## Choice Callbacks

### Setting Up Callbacks

Handle player choices with callbacks:

```csharp
// Purchase callback
Dialogue.OnChoiceSelected("PURCHASE", () => {
    const float price = 100f;
    var balance = Money.GetCashBalance();
    if (balance >= price) {
        Money.ChangeCashBalance(-price, visualizeChange: true, playCashSound: true);
        Dialogue.JumpTo("ShopDialogue", "PURCHASE_COMPLETE");
    } else {
        Dialogue.JumpTo("ShopDialogue", "NOT_ENOUGH_CASH");
    }
});

// Information callback
Dialogue.OnChoiceSelected("GET_INFO", () => {
    // Give player information
    SendTextMessage("Here's the information you requested!");
    Dialogue.JumpTo("InfoDialogue", "INFO_GIVEN");
});

// Relationship callback
Dialogue.OnChoiceSelected("COMPLIMENT", () => {
    Relationship.Add(0.5f); // Increase relationship
    Dialogue.JumpTo("SocialDialogue", "COMPLIMENT_RESPONSE");
});
```

### Callback Best Practices

- **Handle success and failure cases**
- **Use `Dialogue.JumpTo()` to navigate programmatically**
- **Don't call `Dialogue.StopOverride()` from `OnNodeDisplayed`** (causes infinite recursion)
- **Use `Dialogue.StopOverride()` from `OnChoiceSelected`** when appropriate

## Dynamic Navigation

### Programmatic Navigation

Jump between dialogue nodes without player input:

```csharp
// Jump to specific node
Dialogue.JumpTo("ShopDialogue", "PURCHASE_COMPLETE");

// Jump to different container
Dialogue.JumpTo("InfoDialogue", "ENTRY");
```

### Conditional Navigation

Navigate based on game state:

```csharp
Dialogue.OnChoiceSelected("CHECK_INVENTORY", () => {
    var hasItem = Inventory.HasItem("SpecialItem");
    if (hasItem) {
        Dialogue.JumpTo("QuestDialogue", "HAS_ITEM");
    } else {
        Dialogue.JumpTo("QuestDialogue", "NO_ITEM");
    }
});
```

## Dialogue Events

### Node Events

Subscribe to dialogue events:

```csharp
// When a node is displayed
Dialogue.OnNodeDisplayed("PURCHASE_COMPLETE", () => {
    Debug.Log("Purchase completed!");
    // Don't call StopOverride() here!
});

// When a choice is selected
Dialogue.OnChoiceSelected("LEAVE", () => {
    Debug.Log("Player chose to leave");
    Dialogue.StopOverride(); // Safe to call here
});
```

### Important: StopOverride Usage

**Safe**: Call `Dialogue.StopOverride()` from `Dialogue.OnChoiceSelected(...)` when you want a temporary container to stop after a choice.

**Unsafe**: Do NOT call `Dialogue.StopOverride()` from `Dialogue.OnNodeDisplayed(...)` — it re-displays the current node and re-fires the event, causing infinite recursion and a stack overflow.

If you hit this, the stack may look like:
```
Stack overflow.
   at UnityEngine.GameObject.GetComponentInChildren(...)
   at S1API.Entities.NPCDialogue.get_Handler()
   at S1API.Entities.NPCDialogue.StopOverride()
   at S1API.Entities.NPCDialogue.<callback>()
   at S1API.Entities.NPCDialogue.Internal_OnNode(System.String)
```

## Advanced Features

### Multiple Containers

Use different containers for different contexts:

```csharp
// Shop dialogue
Dialogue.BuildAndRegisterContainer("ShopDialogue", c => {
    c.AddNode("ENTRY", "Welcome to my shop!", ch => {
        ch.Add("BUY", "I want to buy", "BUY_MENU");
    });
});

// Social dialogue
Dialogue.BuildAndRegisterContainer("SocialDialogue", c => {
    c.AddNode("ENTRY", "How are you doing?", ch => {
        ch.Add("GOOD", "I'm doing well", "GOOD_RESPONSE");
    });
});

// Quest dialogue
Dialogue.BuildAndRegisterContainer("QuestDialogue", c => {
    c.AddNode("ENTRY", "I have a task for you", ch => {
        ch.Add("ACCEPT", "I'll help", "QUEST_ACCEPTED");
    });
});
```

### Context-Sensitive Dialogue

Change dialogue based on game state:

```csharp
Dialogue.OnChoiceSelected("CHECK_STATUS", () => {
    var relationship = Relationship.Delta;
    if (relationship >= 3.0f) {
        Dialogue.JumpTo("FriendlyDialogue", "ENTRY");
    } else if (relationship >= 1.0f) {
        Dialogue.JumpTo("NeutralDialogue", "ENTRY");
    } else {
        Dialogue.JumpTo("HostileDialogue", "ENTRY");
    }
});
```

### Dynamic Text

Use dynamic text based on game state:

```csharp
Dialogue.BuildAndRegisterContainer("DynamicDialogue", c => {
    c.AddNode("ENTRY", () => {
        var cash = Money.GetCashBalance();
        return $"You have ${cash:F2}. What would you like to do?";
    }, ch => {
        ch.Add("SPEND", "Spend money", "SPEND_MENU");
    });
});
```

## Best Practices

### Do's

- **Use meaningful node and choice names** for debugging
- **Handle all possible outcomes** in choice callbacks
- **Test dialogue flows thoroughly** to ensure they work correctly
- **Use `Dialogue.JumpTo()` for programmatic navigation**
- **Call `Dialogue.StopOverride()` from choice callbacks** when appropriate

### Don'ts

- **Don't call `Dialogue.StopOverride()` from `OnNodeDisplayed`** (causes infinite recursion)
- **Don't create infinite loops** in dialogue flows
- **Don't forget to handle error cases** in choice callbacks
- **Don't use overly complex dialogue trees** that are hard to maintain

### Error Handling

Wrap dialogue configuration in try-catch blocks:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    try
    {
        Dialogue.BuildAndRegisterContainer("MyDialogue", c => {
            // Dialogue configuration
        });
        
        Dialogue.OnChoiceSelected("MY_CHOICE", () => {
            // Choice handling
        });
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"Failed to set up dialogue for {FullName}: {ex.Message}");
    }
}
```

### Performance Considerations

- **Keep dialogue trees reasonable** - overly complex trees can impact performance
- **Use efficient choice callbacks** - avoid expensive operations in callbacks
- **Test with multiple NPCs** - ensure dialogue works well together
- **Monitor dialogue performance** in multiplayer environments

## Next Steps

Now that you understand the dialogue system, explore:

- **[Customer Behavior](customer-behavior.md)** - Customer system details
- **[Relationship Management](relationship-management.md)** - Relationship system
- **[Runtime Management](runtime-management.md)** - NPC lifecycle and properties
