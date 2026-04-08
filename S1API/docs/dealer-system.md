# Dealer System

S1API provides a comprehensive dealer system that allows you to create custom NPCs who can work as dealers for the player, distributing products to customers and managing contracts.

## Overview

Dealer NPCs are special NPCs that can:
- Accept recruitment by the player
- Take on distribution contracts
- Deliver products to customers
- Keep a cut of the profits
- Recommend other dealers
- Have customizable signing fees and payment structures

## Creating a Dealer NPC

To create a dealer NPC, set `IsDealer = true` and configure dealer defaults using the `EnsureDealer()` builder method:

```csharp
using S1API.Entities;
using S1API.Economy;
using S1API.Entities.NPCs.Suburbia;
using UnityEngine;

public class MyDealerNPC : NPC
{
    public override bool IsPhysical => true;
    public override bool IsDealer => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithIdentity("my_dealer", "John", "Distributor")
            .WithSpawnPosition(new Vector3(0, 0, 0))
            .WithAppearanceDefaults(av =>
            {
                av.Gender = 0.0f;
                av.Height = 1.0f;
            })
            .EnsureDealer()
            .WithDealerDefaults(dd =>
            {
                dd.WithSigningFee(1000f)              // Cost to recruit
                  .WithCut(0.15f)                      // Dealer keeps 15%
                  .WithDealerType(DealerType.PlayerDealer)
                  .WithHomeName("North Apartments")    // Home building
                  .AllowInsufficientQuality(false)     // Won't sell low quality
                  .AllowExcessQuality(true)            // Can sell high quality
                  .WithCompletedDealsVariable("my_dealer_deals")
                  .WithRecommendation(r => r
                      .FromCustomer(NPC.Get<JeremyWilkinson>())
                      .OnDealCompleted());
            })
            .WithRelationshipDefaults(r =>
            {
                r.WithDelta(2.0f)
                 .SetUnlocked(false)
                 .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
            });
    }

    protected override void OnCreated()
    {
        base.OnCreated();
        Appearance.Build();

        // Wire up dealer events
        WireDealerEvents();

        Aggressiveness = 2f;
        Region = Region.Northtown;
        Schedule.Enable();
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        UnwireDealerEvents();
    }

    private void WireDealerEvents()
    {
        if (Dealer == null) return;

        Dealer.OnRecruited += HandleRecruited;
        Dealer.OnContractAccepted += HandleContractAccepted;
        Dealer.OnRecommended += HandleRecommended;
    }

    private void UnwireDealerEvents()
    {
        if (Dealer == null) return;

        Dealer.OnRecruited -= HandleRecruited;
        Dealer.OnContractAccepted -= HandleContractAccepted;
        Dealer.OnRecommended -= HandleRecommended;
    }

    private void HandleRecruited()
    {
        SendTextMessage("I'm ready to work for you!");
    }

    private void HandleContractAccepted()
    {
        SendTextMessage("I've got a new contract to handle.");
    }

    private void HandleRecommended()
    {
        SendTextMessage("Thanks for the recommendation!");
    }
}
```

## Dealer Configuration Options

### WithDealerDefaults() Methods

The `DealerDataBuilder` provides several configuration methods:

```csharp
.WithDealerDefaults(dd =>
{
    // Recruitment fee paid by player
    dd.WithSigningFee(1000f)

    // Percentage of profits the dealer keeps
    .WithCut(0.15f)  // 15% commission

    // Type of dealer
    .WithDealerType(DealerType.PlayerDealer)  // Works for player

    // Home building name (where dealer lives)
    .WithHomeName("North Apartments")

    // Quality control
    .AllowInsufficientQuality(false)  // Rejects low quality products
    .AllowExcessQuality(true)         // Accepts high quality products

    // Variable to track completed deals
    .WithCompletedDealsVariable("dealer_completed_deals")

    // Automatically recommend this dealer when the customer completes a deal
    .WithRecommendation(r => r
        .FromCustomer(NPC.Get<JeremyWilkinson>())
        .OnDealCompleted())
});
```

This helper wires the configured customer's `OnDealCompleted` event to the dealer recommendation flow for you. It does not replace relationship unlock metadata such as `UnlockType`; it simply streamlines the separate native recommendation step that the base game requires for dealers.

### Dealer Types

```csharp
public enum DealerType
{
    PlayerDealer,      // Works for the player (standard)
    IndependentDealer, // Independent operation
    RivalDealer        // Works for competition
}
```

## Dealer Events

Dealers provide several events you can subscribe to:

### OnRecruited

Fired when the dealer is successfully recruited by the player:

```csharp
Dealer.OnRecruited += () =>
{
    MelonLogger.Msg($"Dealer {ID} has been recruited!");
    SendTextMessage("I'm ready to start working!");
};
```

### OnContractAccepted

Fired when the dealer accepts a new distribution contract:

```csharp
Dealer.OnContractAccepted += () =>
{
    MelonLogger.Msg($"Dealer {ID} accepted a contract");
    SendTextMessage("I've got a new job to do.");
};
```

### OnRecommended

Fired when the dealer is recommended to another NPC:

```csharp
Dealer.OnRecommended += () =>
{
    MelonLogger.Msg($"Dealer {ID} was recommended");
    SendTextMessage("Thanks for spreading the word!");
};
```

## Complete Dealer Example

Here's a complete example with all dealer features:

```csharp
using System;
using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.Schedule;
using S1API.Entities.NPCs.Northtown;
using S1API.Map.Buildings;
using UnityEngine;

public sealed class ProfessionalDealer : NPC
{
    public override bool IsPhysical => true;
    public override bool IsDealer => true;

    private Action _dealerRecruitedHandler;
    private Action _dealerContractAcceptedHandler;
    private Action _dealerRecommendedHandler;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        var homeBuilding = Building.Get<NorthApartments>();
        Vector3 homePosition = new Vector3(-28f, 1.065f, 62f);
        Vector3 spawnPos = new Vector3(-53f, 1.065f, 68f);

        builder.WithIdentity("professional_dealer", "Marcus", "Stone")
            .WithSpawnPosition(spawnPos)
            .WithAppearanceDefaults(av =>
            {
                av.Gender = 0.0f;
                av.Height = 1.1f;
                av.Weight = 0.6f;
                var skinColor = new Color32(140, 110, 90, 255);
                av.SkinColor = skinColor;
                av.LeftEyeLidColor = av.SkinColor;
                av.RightEyeLidColor = av.SkinColor;
                av.HairColor = new Color(0.1f, 0.1f, 0.1f);
                av.HairPath = "Avatar/Hair/Buzzcut/Buzzcut";
                av.WithBodyLayer("Avatar/Layers/Top/T-Shirt", new Color(0.1f, 0.1f, 0.1f));
                av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.2f, 0.2f, 0.3f));
                av.WithAccessoryLayer("Avatar/Accessories/Feet/Sneakers/Sneakers", Color.black);
            })
            .EnsureDealer()
            .WithDealerDefaults(dd =>
            {
                dd.WithSigningFee(2500f)  // Higher fee = more experienced
                  .WithCut(0.12f)          // Lower cut = more loyal
                  .WithDealerType(DealerType.PlayerDealer)
                  .WithHomeName("North Apartments")
                  .AllowInsufficientQuality(false)  // Quality-focused
                  .AllowExcessQuality(true)
                  .WithCompletedDealsVariable("professional_dealer_deals");
            })
            .WithRelationshipDefaults(r =>
            {
                r.WithDelta(3.0f)  // Starts with good relationship
                 .SetUnlocked(false)
                 .SetUnlockType(NPCRelationship.UnlockType.DirectApproach)
                 .WithConnections(Get<KyleCooley>(), Get<LudwigMeyer>());
            })
            .WithSchedule(plan =>
            {
                plan.EnsureDealSignal()  // Required for dealer functionality
                    .WalkTo(homePosition, 800)
                    .StayInBuilding(homeBuilding, 1000, 120)
                    .WalkTo(spawnPos, 1200)
                    .LocationDialogue(spawnPos, 1400);
            })
            .WithInventoryDefaults(inv =>
            {
                inv.WithStartupItems("baseballbat")
                   .WithRandomCash(min: 200, max: 800)
                   .WithClearInventoryEachNight(false);
            });
    }

    public ProfessionalDealer() : base()
    {
    }

    protected override void OnCreated()
    {
        try
        {
            base.OnCreated();
            Appearance.Build();

            SendTextMessage("Looking for work. Got product to move?");
            WireDealerEvents();

            Aggressiveness = 2f;
            Region = Region.Northtown;
            Schedule.Enable();
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"ProfessionalDealer OnCreated failed: {ex.Message}");
        }
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        UnwireDealerEvents();
    }

    private void WireDealerEvents()
    {
        if (Dealer == null)
        {
            MelonLogger.Warning($"Dealer component missing for {ID}");
            return;
        }

        _dealerRecruitedHandler ??= HandleDealerRecruited;
        _dealerContractAcceptedHandler ??= HandleContractAccepted;
        _dealerRecommendedHandler ??= HandleDealerRecommended;

        Dealer.OnRecruited -= _dealerRecruitedHandler;
        Dealer.OnRecruited += _dealerRecruitedHandler;

        Dealer.OnContractAccepted -= _dealerContractAcceptedHandler;
        Dealer.OnContractAccepted += _dealerContractAcceptedHandler;

        Dealer.OnRecommended -= _dealerRecommendedHandler;
        Dealer.OnRecommended += _dealerRecommendedHandler;
    }

    private void UnwireDealerEvents()
    {
        if (Dealer == null) return;

        if (_dealerRecruitedHandler != null)
            Dealer.OnRecruited -= _dealerRecruitedHandler;

        if (_dealerContractAcceptedHandler != null)
            Dealer.OnContractAccepted -= _dealerContractAcceptedHandler;

        if (_dealerRecommendedHandler != null)
            Dealer.OnRecommended -= _dealerRecommendedHandler;
    }

    private void HandleDealerRecruited()
    {
        MelonLogger.Msg($"Dealer {ID} recruited!");
        SendTextMessage("I won't let you down. I'm professional.");
    }

    private void HandleContractAccepted()
    {
        MelonLogger.Msg($"Dealer {ID} accepted new contract");
        SendTextMessage("Got the contract. I'll handle it.");
    }

    private void HandleDealerRecommended()
    {
        MelonLogger.Msg($"Dealer {ID} recommended");
        SendTextMessage("Appreciate the recommendation.");
    }
}
```

## Dealer Schedule

Dealers must have `EnsureDealSignal()` in their schedule to properly handle distribution contracts:

```csharp
.WithSchedule(plan =>
{
    plan.EnsureDealSignal()  // REQUIRED for dealer functionality
        .WalkTo(position, time)
        .StayInBuilding(building, time);
});
```

Without `EnsureDealSignal()`, the dealer won't be able to accept or complete contracts.

## Best Practices

1. **Always Include EnsureDealSignal()**: Dealers need this in their schedule to function properly

2. **Event Cleanup**: Unsubscribe from dealer events in `OnDestroyed()` to prevent memory leaks

3. **Quality Control**: Configure `AllowInsufficientQuality` and `AllowExcessQuality` based on dealer personality
   - Professional dealers should reject low quality (`AllowInsufficientQuality(false)`)
   - Desperate dealers might accept anything

4. **Signing Fees**: Balance signing fees with cut percentages
   - Higher signing fee + lower cut = one-time investment
   - Lower signing fee + higher cut = ongoing cost

5. **Home Building**: Always set a valid home building name that exists in the game

6. **Relationships**: Start dealers with `SetUnlocked(false)` so players must discover them

## Common Issues

### Dealer Not Accepting Contracts

**Problem**: Dealer isn't accepting contracts when assigned.

**Solution**: Ensure `EnsureDealSignal()` is called in the schedule configuration:

```csharp
.WithSchedule(plan =>
{
    plan.EnsureDealSignal()  // This is required!
        .WalkTo(position, time);
});
```

### Events Not Firing

**Problem**: Dealer events (`OnRecruited`, etc.) aren't being called.

**Solution**: Check that:
1. You're subscribing to events after `base.OnCreated()`
2. The `Dealer` component isn't null before subscribing
3. You're not unsubscribing prematurely

## Complete Dealer Example

For a production-ready dealer NPC implementation, see **[ExamplePhysicalDealerNPC](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalDealerNPC.cs)** from the S1API NPC Example Repository.

This example includes dealer defaults, a dealer-ready schedule, and safe event subscribe/unsubscribe patterns.

## See Also

- **[ExamplePhysicalDealerNPC](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalDealerNPC.cs)** - Reference implementation
- [Custom NPCs](custom-npcs.md) - Core NPC creation guide
- [Customer Behavior](customer-behavior.md) - For creating customer NPCs
- <xref:S1API.Economy> - Economy API Reference
- <xref:S1API.Entities.Dealer> - Dealer API Reference
