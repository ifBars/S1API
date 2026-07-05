---
title: "Economy System"
description: "Reference for the economy system — customers, dealers, suppliers, and dead drops"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-05"
applies_to: "Schedule I v0.4.3+"
---

# Economy System

## Overview

The economy system handles customers, dealers, suppliers, and dead drops — the core drug-dealing loop.

## Key Classes

| Class | Namespace | Purpose |
|---|---|---|
| `Customer` | `ScheduleOne.Economy` | Drug buyers with addiction, affinity, satisfaction |
| `Dealer` | `ScheduleOne.Economy` | Player-hired dealers that sell to assigned customers |
| `Supplier` | `ScheduleOne.Economy` | NPCs that supply items via dead drops |
| `DeadDrop` | `ScheduleOne.Economy` | Drop locations for supplier deliveries |
| `CustomerSatisfaction` | `ScheduleOne.Economy` | Satisfaction → relationship change formula |
| `DeliveryLocation` | `ScheduleOne.Economy` | Handover locations for deals |

## Customers

`Il2CppScheduleOne.Economy.Customer`

- Static lists: `Customer.UnlockedCustomers`, `Customer.LockedCustomers`
- Properties: `CurrentAddiction` (0–1), `AssignedDealer`, `CurrentContract`, `IsAwaitingDelivery`
- Affinity: Per-drug-type preferences via `GetAffinity(EDrugType)`
- Events: `onCustomerUnlocked`, `onDealCompleted`

## Dealers

`Il2CppScheduleOne.Economy.Dealer`

- Static list: `Dealer.AllPlayerDealers`
- Properties: `Cash` (earned, collectible), `Cut` (commission), `AssignedCustomers`
- Functions: `AddCustomer`, `RemoveCustomer`, `GetOrderableProducts`
- Events: `onDealerRecruited`

## Suppliers

`Il2CppScheduleOne.Economy.Supplier`

- Status flow: `Idle` → `PreppingDeadDrop` → `Meeting`
- Properties: `Debt`, `DeliveriesEnabled`, `minsUntilDeaddropReady`
- DeadDrop ordering via MessagesApp

## Dead Drops

`Il2CppScheduleOne.Economy.DeadDrop`

- Static list: `DeadDrop.DeadDrops`
- Storage: `WorldStorageEntity` with `ItemSlots`
- `GetRandomEmptyDrop(Vector3)` to find nearest free drop

## S1Toolkit API

Use `Api.Economy.*` instead of raw Il2Cpp classes:

| Method | Description |
|---|---|
| `GetUnlockedCustomers()` | All unlocked customer IDs |
| `GetCustomerAddiction(id)` | Addiction level (0–1) |
| `GetPlayerDealers()` | All player-hired dealer IDs |
| `GetDealerCash(id)` | Dealer's earned cash |
| `GetSuppliers()` | All supplier IDs |
| `GetSupplierDebt(id)` | Current debt amount |
| `GetDeadDrops()` | All dead drop names |
| `GetNearestEmptyDeadDrop()` | Nearest free dead drop |

See [api-reference.md](../api/api-reference.md) for the complete API.
