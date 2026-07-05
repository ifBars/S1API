---
title: "Networking & Lobby System"
description: "Reference for the networking and lobby system — FishNet, lobby management, Steamworks, and multiplayer architecture"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Networking & Lobby System

## Lobby (`Il2CppScheduleOne.Networking.Lobby`)
`PersistentSingleton<Lobby>`. Steam lobby management.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `ENABLED` | `true` | Steam lobby system enabled |
| `PLAYER_LIMIT` | 4 | Max players |
| `JOIN_READY` | `"ready"` | Ready state string |
| `LOAD_TUTORIAL` | `"load_tutorial"` | Tutorial mode string |
| `HOST_LOADING` | `"host_loading"` | Host loading state |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `Players` | `CSteamID[4]` | Player Steam IDs |
| `onLobbyChange` | `Action` | Called on lobby state change |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `IsHost` | `bool` (get) | Whether this client is host |
| `LobbyID` | `ulong` (get) | Steam lobby ID |
| `LobbySteamID` | `CSteamID` (get) | SteamID wrapper |
| `IsInLobby` | `bool` (get) | Whether in a lobby |
| `PlayerCount` | `int` (get) | Connected player count |
| `LocalPlayerID` | `CSteamID` (get) | Local user's Steam ID |

### Callbacks (Steam)
| Callback | Event |
|----------|-------|
| `LobbyCreatedCallback` | `LobbyCreated_t` |
| `LobbyEnteredCallback` | `LobbyEnter_t` |
| `ChatUpdateCallback` | `LobbyChatUpdate_t` |
| `GameLobbyJoinRequestedCallback` | `GameLobbyJoinRequested_t` |
| `LobbyChatMessageCallback` | `LobbyChatMsg_t` |

### Methods
| Method | Description |
|--------|-------------|
| `CreateLobby()` | Create new lobby |
| `LeaveLobby()` | Leave current lobby |
| `GetPlayerSteamID(int index)` | Get Steam ID by slot |

---

## AutoNetworkStart (`Il2CppScheduleOne.Networking.AutoNetworkStart`)
MonoBehaviour handling automatic FishNet start.

---

## NetworkConditionalObject (`Il2CppScheduleOne.Networking.NetworkConditionalObject`)
NetworkObject that activates based on conditions (e.g., only for host).

---

## ReplicationQueue (`Il2CppScheduleOne.Networking.ReplicationQueue`)
Manages staggered network replication.

---

## TransportInitializer (`Il2CppScheduleOne.Networking.TransportInitializer`)
Initializes the Steam transport (FishySteamworks).

---

## IStaggeredReplicator (`Il2CppScheduleOne.Networking.IStaggeredReplicator`)
Interface for objects that replicate in staggered order.

---

## LocalMultiplayerTool (`Il2CppScheduleOne.Networking.LocalMultiplayerTool`)
Editor/debug tool for testing multiplayer locally.

---

## FishNet Network Configuration

- **Framework**: FishNet (full Fish-Networking)
- **Transport**: FishySteamworks (Steam P2P)
- **Sync Types**: SyncVars, SyncLists, ServerRpc, ObserversRpc, TargetRpc
- **Scene Management**: FishNet SceneManager
- **Object Pooling**: FishNet ObjectPool
- **Serialization**: FishNet auto-generated writers/readers for Il2Cpp types

---

## Key Network Safety Rules

| Rule | Reason |
|------|--------|
| Only server calls `InstanceFinder.IsServer` guarded code | Clients don't have authority |
| `ObserversRpc(RunLocally = true)` for host-side execution | Prevents double execution |
| Always check `base.IsServerInitialized` | Object may not have NetworkObject yet |
| SyncVar reads may return stale data | Use RPCs for critical state sync |
| Non-networked objects use `network = false` in RPC-style methods | Prevents null-overwrite from FishNet |

---

## Usage Patterns

### Checking network role
```csharp
if (InstanceFinder.IsServer)
{
    // Server-only logic
}

if (InstanceFinder.IsHost)
{
    // Host-specific (server + client)
}

if (base.IsClientInitialized && !base.IsHost)
{
    // Pure client (not host)
}
```

### Lobby checks
```csharp
if (Singleton<Lobby>.Instance.IsInLobby)
{
    int playerCount = Singleton<Lobby>.Instance.PlayerCount;
    bool amHost = Singleton<Lobby>.Instance.IsHost;
}
```

### Safe RPC pattern (Il2Cpp Harmony)
```csharp
[HarmonyPrefix]
[HarmonyPatch(typeof(MyNetworkBehaviour), nameof(MyNetworkBehaviour.MyRpc))]
static bool Prefix()
{
    if (!InstanceFinder.IsServer) return false; // Suppress on clients
    return true; // Let original run on server
}
```

### Custom ServerRpc wrapper
```csharp
// Can't add RPCs to Il2Cpp types via Harmony
// Instead, call the existing RPC methods if available
// Or use NetworkManager to invoke observers

if (InstanceFinder.IsServer)
{
    // Direct logic
    ObserversRpcTarget(target, data);
}
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.Session.IsHost()` | Check if host |
| `Api.Session.IsClient()` | Check if client |
| `Api.Session.IsMultiplayer()` | Check if multiplayer |
| `Api.Session.GetPlayerCount()` | Get player count |
