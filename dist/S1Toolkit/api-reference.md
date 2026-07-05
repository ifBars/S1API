# S1Toolkit API – Quick Reference

## Quickstart (Top 3)

```csharp
Api.Money.Add(1000);                          // +1000 cash
Api.Inventory.Add("ogkush", 5);               // 5x weed into inventory
Api.Player.SetHealth(100);                     // health to 100
```

---

## 🟢 Core APIs

### Api.Player

| Method | Description | Example |
|---|---|---|
| `GetHealth()` | Returns current health (0–100) | `float h = Api.Player.GetHealth();` |
| `SetHealth(v)` | Sets the player's health | `Api.Player.SetHealth(80);` |
| `GetEnergy()` | Returns current energy (0–100) | `float e = Api.Player.GetEnergy();` |
| `SetEnergy(v)` | Sets the player's energy | `Api.Player.SetEnergy(100);` |
| `GetCash()` | Returns current cash | `float cash = Api.Player.GetCash();` |
| `AddCash(v)` | Adds cash (negative = deduct) | `Api.Player.AddCash(500);` |
| `IsArrested()` | Checks whether the player is arrested | `bool a = Api.Player.IsArrested();` |
| `IsAlive()` | Checks whether the player is alive | `if (!Api.Player.IsAlive()) return;` |
| `Teleport(x,y,z)` | Teleports the player to a world position | `Api.Player.Teleport(0, 1, 0);` |
| `IsMoving()` | Checks whether the player is moving | `bool m = Api.Player.IsMoving();` |
| `GetTimeSinceLastDamage()` | Seconds since last damage taken | `float t = Api.Player.GetTimeSinceLastDamage();` |

### Api.Inventory

| Method | Description | Example |
|---|---|---|
| `Add(id, qty=1)` | Adds items to the inventory | `Api.Inventory.Add("cocaine", 10);` |
| `Remove(id, qty=1)` | Removes items from the inventory | `Api.Inventory.Remove("cocaine", 3);` |
| `GetQuantity(id)` | Returns the count of an item in the inventory | `int n = Api.Inventory.GetQuantity("ogkush");` |
| `HasItem(id)` | Checks whether an item is in the inventory | `if (Api.Inventory.HasItem("jar"))` |
| `GetEquippedSlotIndex()` | Index of the equipped hotbar slot | `int s = Api.Inventory.GetEquippedSlotIndex();` |
| `GetEquippedItemId()` | Item ID of the equipped item | `string id = Api.Inventory.GetEquippedItemId();` |
| `EquipSlot(idx)` | Equips a hotbar slot by index | `Api.Inventory.EquipSlot(0);` |

### Api.Item

| Method | Description | Example |
|---|---|---|
| `GetName(id)` | Returns an item's display name | `string n = Api.Item.GetName("ogkush");` |
| `GetCategory(id)` | Returns the item category | `var c = Api.Item.GetCategory("ogkush");` |
| `GetQuality(id)` | Returns an item's quality tier | `var q = Api.Item.GetQuality("ogkush");` |
| `GetMonetaryValue(id)` | Base purchase price of the item in dollars | `float p = Api.Item.GetMonetaryValue("cocaine");` |
| `Exists(id)` | Checks whether an item with this ID is registered | `if (Api.Item.Exists("meth"))` |

### Api.Money

| Method | Description | Example |
|---|---|---|
| `GetBalance()` | Returns current cash | `float b = Api.Money.GetBalance();` |
| `GetOnlineBalance()` | Returns online balance (bank account) | `float o = Api.Money.GetOnlineBalance();` |
| `GetLifetimeEarnings()` | Returns total earnings over the playthrough | `float e = Api.Money.GetLifetimeEarnings();` |
| `Add(amount)` | Adds cash | `Api.Money.Add(5000);` |
| `Remove(amount)` | Removes cash if enough is available | `Api.Money.Remove(200);` |
| `TransferToOnline(a)` | Transfers cash to the bank account | `Api.Money.TransferToOnline(1000);` |
| `TransferToCash(a)` | Transfers bank balance to cash | `Api.Money.TransferToCash(500);` |

### Api.Time

| Method | Description | Example |
|---|---|---|
| `GetCurrentMinute()` | Minutes since midnight (0–1440) | `int m = Api.Time.GetCurrentMinute();` |
| `GetCurrentHour()` | Current in-game hour (0–23) | `int h = Api.Time.GetCurrentHour();` |
| `GetElapsedDays()` | In-game days elapsed since game start | `int d = Api.Time.GetElapsedDays();` |
| `GetDayName()` | English weekday name (e.g. "Monday") | `string dn = Api.Time.GetDayName();` |
| `IsNight()` | Checks whether it is currently night | `if (Api.Time.IsNight())` |
| `IsEndOfDay()` | Checks whether end of day (4 AM stop) is reached | `if (Api.Time.IsEndOfDay())` |
| `SetTime(minute)` | Sets the game time (host only) | `Api.Time.SetTime(480);` |
| `SkipToMorning()` | Jumps to 6:00 AM | `Api.Time.SkipToMorning();` |
| `SkipToNight()` | Jumps to 10:00 PM | `Api.Time.SkipToNight();` |
| `OnMinutePass` | Event: every in-game minute | `Api.Time.OnMinutePass += () => …;` |
| `OnHourPass` | Event: every full in-game hour | `Api.Time.OnHourPass += () => …;` |
| `OnDayPass` | Event: on day change | `Api.Time.OnDayPass += () => …;` |
| `OnSleepStart` | Event: player starts sleeping | `Api.Time.OnSleepStart += () => …;` |
| `OnSleepEnd` | Event: player wakes up | `Api.Time.OnSleepEnd += () => …;` |

### Api.Session

| Method | Description | Example |
|---|---|---|
| `IsHost()` | Checks whether the player is host (singleplayer = host) | `if (Api.Session.IsHost())` |
| `IsClient()` | Checks whether the player is a co-op guest only | `if (Api.Session.IsClient())` |
| `IsMultiplayer()` | At least 2 players in the session | `if (Api.Session.IsMultiplayer())` |
| `IsInLobby()` | Checks whether a Steam lobby is active | `if (Api.Session.IsInLobby())` |
| `GetPlayerCount()` | Number of players in the session | `int p = Api.Session.GetPlayerCount();` |
| `OnPlayerJoined` | Event: player joins the session | `Api.Session.OnPlayerJoined += () => …;` |
| `OnPlayerLeft` | Event: player leaves the session | `Api.Session.OnPlayerLeft += () => …;` |

### Api.Save

| Method | Description | Example |
|---|---|---|
| `SaveData(key, json)` | Stores a JSON string for the current savegame | `Api.Save.SaveData("my_mod", "{\"x\":1}");` |
| `LoadData(key)` | Loads a stored JSON string | `string d = Api.Save.LoadData("my_mod");` |
| `GetCurrentSaveId()` | ID (name) of the current savegame | `string id = Api.Save.GetCurrentSaveId();` |
| `IsSaving()` | Checks whether the game is currently saving | `if (Api.Save.IsSaving()) return;` |

### Api.Config

| Method | Description | Example |
|---|---|---|
| `Get<T>(cat, key, def)` | Reads a config value with default fallback | `bool b = Api.Config.Get("MyMod", "enabled", true);` |
| `Set<T>(cat, key, val)` | Sets a config value in memory | `Api.Config.Set("MyMod", "enabled", false);` |
| `Save(cat)` | Writes a category to file | `Api.Config.Save("MyMod");` |
| `RegisterOnChange(cat, key, cb)` | Registers a callback on value change | `Api.Config.RegisterOnChange("MyMod","k",()=>…);` |

### Api.UI

| Method | Description | Example |
|---|---|---|
| `Notify(title, text)` | Sends a notification with title and text | `Api.UI.Notify("Mod", "Done!");` |
| `ShowNotification(msg)` | Shows a simple notification | `Api.UI.ShowNotification("Hello");` |
| `IsSleeping()` | Checks whether the sleep menu is open | `if (Api.UI.IsSleeping())` |
| `ForceSleep()` | Forces the sleep sequence | `Api.UI.ForceSleep();` |
| `ShowArrestScreen()` | Shows the arrest screen | `Api.UI.ShowArrestScreen();` |
| `ShowDialogue(sp, txt, ch)` | Shows a dialogue with speaker and choices | `Api.UI.ShowDialogue("NPC","Hello",new[]{"OK"});` |
| `GetDialogueChoice()` | Index of the chosen dialogue answer (–1 = none) | `int c = Api.UI.GetDialogueChoice();` |

---

## 🟡 Gameplay APIs

### Api.NPC

| Method | Description | Example |
|---|---|---|
| `GetAllNpcNames()` | Keys of all registered NPCs | `string[] n = Api.NPC.GetAllNpcNames();` |
| `GetRelationship(name)` | Relationship value 0–5 (0=hostile, 5=loyal) | `float r = Api.NPC.GetRelationship("ming");` |
| `SetRelationship(name, v)` | Sets the relationship value (0–5) | `Api.NPC.SetRelationship("ming",5);` |
| `GetRelationshipCategory(name)` | Relationship tier as enum | `var rc = Api.NPC.GetRelationshipCategory("ming");` |
| `Teleport(name, x, y, z)` | Teleports an NPC to a world position | `Api.NPC.Teleport("ming",0,2,0);` |
| `IsMoving(name)` | Checks whether the NPC is moving | `bool m = Api.NPC.IsMoving("ming");` |
| `IsDead(name)` | Checks whether the NPC is dead | `if (Api.NPC.IsDead("ming"))` |
| `IsKnockedOut(name)` | Checks whether the NPC is unconscious | `if (Api.NPC.IsKnockedOut("ming"))` |
| `GetCurrentBehaviour(name)` | Name of the active behaviour | `string b = Api.NPC.GetCurrentBehaviour("ming");` |
| `GetCurrentBuilding(name)` | Name of the building the NPC is in | `string b = Api.NPC.GetCurrentBuilding("ming");` |
| `OnNPCDeath` | Event: NPC dies (parameter: NPC key) | `Api.NPC.OnNPCDeath += id => …;` |
| `OnNPCRelationshipChange` | Event: relationship changes | `Api.NPC.OnNPCRelationshipChange += id => …;` |

### Api.NpcBehaviour

| Method | Description | Example |
|---|---|---|
| `SetBehaviour(name, type)` | Sets behaviour (Idle, Flee, Pursue) | `Api.NpcBehaviour.SetBehaviour("ming",Api.Types.BehaviourType.Flee);` |
| `SetPatrolRoute(name, pts)` | Sets a patrol route | `Api.NpcBehaviour.SetPatrolRoute("ming",new[]{0,2,3,4,2,3});` |
| `SetDestination(name,x,y,z)` | Sends the NPC to a world position via NavMesh | `Api.NpcBehaviour.SetDestination("ming",10,0,5);` |
| `StartPursuit(name,player)` | Makes the NPC pursue a player | `Api.NpcBehaviour.StartPursuit("ming","");` |
| `StartFlee(name,x,y,z)` | Makes the NPC flee from a position | `Api.NpcBehaviour.StartFlee("ming",0,0,0);` |
| `Stop(name)` | Stops movement and active behaviour | `Api.NpcBehaviour.Stop("ming");` |
| `Pause(name)` | Pauses NPC movement | `Api.NpcBehaviour.Pause("ming");` |
| `Resume(name)` | Resumes paused movement | `Api.NpcBehaviour.Resume("ming");` |

### Api.Police

| Method | Description | Example |
|---|---|---|
| `GetPursuitLevel()` | Current pursuit level as CrimeLevel | `var l = Api.Police.GetPursuitLevel();` |
| `GetArrestProgress()` | Arrest progress (0–1) | `float p = Api.Police.GetArrestProgress();` |
| `IsWanted()` | Checks whether the player is wanted | `if (Api.Police.IsWanted())` |
| `GetActiveCrimeCount()` | Number of active crimes | `int c = Api.Police.GetActiveCrimeCount();` |
| `GetActiveCrimes()` | Names of all active crimes | `string[] c = Api.Police.GetActiveCrimes();` |
| `Escalate()` | Escalates the pursuit level by one step | `Api.Police.Escalate();` |
| `Deescalate()` | De-escalates the pursuit level by one step | `Api.Police.Deescalate();` |
| `ClearWarrant()` | Clears the warrant & sets level to None | `Api.Police.ClearWarrant();` |
| `DispatchTo(x,y,z)` | Triggers a police dispatch | `Api.Police.DispatchTo(10,0,5);` |
| `OnArrestStart` | Event: arrest begins | `Api.Police.OnArrestStart += () => …;` |
| `OnArrestEnd` | Event: arrest ends | `Api.Police.OnArrestEnd += () => …;` |
| `OnPursuitLevelChange` | Event: pursuit level changes | `Api.Police.OnPursuitLevelChange += l => …;` |

### Api.Combat

| Method | Description | Example |
|---|---|---|
| `ApplyDamage(amount)` | Applies damage to the local player | `Api.Combat.ApplyDamage(10);` |
| `ApplyDamageTo(npc, a)` | Applies damage to an NPC | `Api.Combat.ApplyDamageTo("ming",25);` |
| `IsDead(npcName)` | Checks whether an NPC is dead | `if (Api.Combat.IsDead("ming"))` |
| `Kill(npcName)` | Kills an NPC instantly | `Api.Combat.Kill("ming");` |
| `IsInvincible()` | Checks whether the player is invincible | `bool inv = Api.Combat.IsInvincible();` |
| `SetInvincible(npc, val)` | Sets an NPC's invincibility | `Api.Combat.SetInvincible("ming", true);` |

### Api.Quest

| Method | Description | Example |
|---|---|---|
| `GetActiveQuests()` | GUIDs of all active quests | `string[] a = Api.Quest.GetActiveQuests();` |
| `GetCompletedQuests()` | GUIDs of all completed quests | `string[] c = Api.Quest.GetCompletedQuests();` |
| `GetQuestTitle(guid)` | Title of a quest by GUID | `string t = Api.Quest.GetQuestTitle("abc");` |
| `GetQuestState(guid)` | State of a quest (Active/Completed/…) | `var s = Api.Quest.GetQuestState("abc");` |
| `TrackQuest(guid)` | Marks a quest as "tracked" (visible on HUD) | `Api.Quest.TrackQuest("abc");` |
| `CompleteQuest(guid)` | Completes a quest | `Api.Quest.CompleteQuest("abc");` |
| `OnQuestCompleted` | Event: quest completed (GUID) | `Api.Quest.OnQuestCompleted += g => …;` |
| `OnQuestAccepted` | Event: quest accepted (GUID) | `Api.Quest.OnQuestAccepted += g => …;` |

### Api.Vehicle

| Method | Description | Example |
|---|---|---|
| `GetOwnedVehicles()` | Codes of all owned vehicles | `string[] v = Api.Vehicle.GetOwnedVehicles();` |
| `Spawn(id, x, y, z)` | Spawns a vehicle at a position | `Api.Vehicle.Spawn("shitbox",0,1,5);` |
| `Enter(id)` | Enters a vehicle | `Api.Vehicle.Enter("shitbox");` |
| `Exit()` | Exits the current vehicle | `Api.Vehicle.Exit();` |
| `Destroy(id)` | Destroys a vehicle | `Api.Vehicle.Destroy("shitbox");` |
| `IsPlayerInVehicle()` | Checks whether the player is in a vehicle | `if (Api.Vehicle.IsPlayerInVehicle())` |
| `GetCurrentVehicle()` | Vehicle code of the current vehicle | `string v = Api.Vehicle.GetCurrentVehicle();` |
| `GetSpeed(id)` | Speed in km/h | `float s = Api.Vehicle.GetSpeed("shitbox");` |

### Api.Growing

| Method | Description | Example |
|---|---|---|
| `Plant(seedId,x,y,z)` | Plants a seed in the nearest free pot | `Api.Growing.Plant("weed_seed",0,1,3);` |
| `GetGrowthProgress(id)` | Growth progress 0–1 | `float g = Api.Growing.GetGrowthProgress("Pot1");` |
| `GetGrowthStage(id)` | Growth stage (1-based) | `int s = Api.Growing.GetGrowthStage("Pot1");` |
| `IsReadyToHarvest(id)` | Checks whether the plant is ready to harvest | `if (Api.Growing.IsReadyToHarvest("Pot1"))` |
| `Harvest(id)` | Harvests all harvestables of the plant | `Api.Growing.Harvest("Pot1");` |
| `Water(id, amount)` | Waters a grow container | `Api.Growing.Water("Pot1", 50);` |
| `AddAdditive(id, addId)` | Adds an additive to the grow container | `Api.Growing.AddAdditive("Pot1","speed_boost");` |

### Api.Property

| Method | Description | Example |
|---|---|---|
| `GetOwnedProperties()` | Codes of all owned properties | `string[] p = Api.Property.GetOwnedProperties();` |
| `IsOwned(name)` | Checks whether a property is owned | `if (Api.Property.IsOwned("bungalow"))` |
| `GetPrice(name)` | Purchase price in dollars | `float p = Api.Property.GetPrice("bungalow");` |
| `Buy(name)` | Buys a property (deducts price from cash) | `Api.Property.Buy("bungalow");` |
| `SetOwned(name, val)` | Sets ownership state (cheat) | `Api.Property.SetOwned("bungalow",true);` |
| `GetPropertyLocation(name)` | Location description as text | `string l = Api.Property.GetPropertyLocation("bungalow");` |

### Api.Weather

| Method | Description | Example |
|---|---|---|
| `GetCurrentWeather()` | Currently dominating weather | `var w = Api.Weather.GetCurrentWeather();` |
| `SetWeather(type)` | Sets the weather (host only) | `Api.Weather.SetWeather(Api.Types.WeatherType.Stormy);` |
| `GetTemperature()` | Temperature at the player's location in °C | `float t = Api.Weather.GetTemperature();` |
| `GetAmbientTemperatureAt(x,y,z)` | Temperature at a world position in °C | `float t = Api.Weather.GetAmbientTemperatureAt(0,1,0);` |

---

## 🔵 Meta APIs

### Api.Crafting

| Method | Description | Example |
|---|---|---|
| `StartMix(station, recipe)` | Starts a mix/cook process | `Api.Crafting.StartMix("LabStation","recipe_1");` |
| `GetMixProgress(station)` | Progress 0–1 of the current process | `float p = Api.Crafting.GetMixProgress("LabStation");` |
| `IsMixComplete(station)` | Checks whether the process is complete | `if (Api.Crafting.IsMixComplete("LabStation"))` |
| `CollectProduct(station)` | Collects the finished product | `Api.Crafting.CollectProduct("LabStation");` |
| `CancelMix(station)` | Cancels the process | `Api.Crafting.CancelMix("LabStation");` |
| `GetAvailableRecipes(type)` | Recipe IDs for a station type | `string[] r = Api.Crafting.GetAvailableRecipes("mixing");` |
| `HasIngredients(recipeId)` | Checks ingredients in the inventory | `bool h = Api.Crafting.HasIngredients("r1");` |
| `GetStationType(id)` | Determines the station type (Chemistry/Mixing/Oven) | `var t = Api.Crafting.GetStationType("LabStation");` |

### Api.Product

| Method | Description | Example |
|---|---|---|
| `GetDiscoveredProducts()` | IDs of all discovered products | `string[] d = Api.Product.GetDiscoveredProducts();` |
| `DiscoverProduct(id)` | Discovers a new product | `Api.Product.DiscoverProduct("new_strain");` |
| `IsProductDiscovered(id)` | Checks whether a product is already discovered | `if (Api.Product.IsProductDiscovered("ogkush"))` |
| `GetPrice(id)` | Current sale price in dollars | `float p = Api.Product.GetPrice("ogkush");` |
| `SetPrice(id, price)` | Sets a new sale price (1–999) | `Api.Product.SetPrice("ogkush",50);` |
| `IsAcceptingOrders()` | Checks whether orders are being accepted | `if (Api.Product.IsAcceptingOrders())` |
| `GetProductList()` | IDs of all known products | `string[] a = Api.Product.GetProductList();` |

### Api.Employees

| Method | Description | Example |
|---|---|---|
| `Hire(npc, type)` | Hires an NPC as an employee | `Api.Employees.Hire("ming",Api.Types.EmployeeType.Botanist);` |
| `Fire(idx)` | Fires an employee by index | `Api.Employees.Fire(0);` |
| `GetEmployeeTypes()` | All available employee types | `var t = Api.Employees.GetEmployeeTypes();` |
| `GetEmployeeCount()` | Number of hired employees | `int n = Api.Employees.GetEmployeeCount();` |
| `GetEmployeeType(idx)` | Job of an employee by index | `var e = Api.Employees.GetEmployeeType(0);` |
| `AssignToProperty(idx,prop)` | Assigns an employee to a property | `Api.Employees.AssignToProperty(0,"bungalow");` |

### Api.Storage

| Method | Description | Example |
|---|---|---|
| `Store(item, qty, cont)` | Moves items from the inventory into a container | `Api.Storage.Store("ogkush",5,"Shelf1");` |
| `Retrieve(item, qty, cont)` | Takes items out of a container | `int n = Api.Storage.Retrieve("ogkush",3,"Shelf1");` |
| `GetQuantity(item, cont)` | Count of an item in the container | `int n = Api.Storage.GetQuantity("ogkush","Shelf1");` |
| `GetContainers()` | Names of all storage containers in the world | `string[] c = Api.Storage.GetContainers();` |
| `GetFreeSlots(cont)` | Number of free slots in the container | `int f = Api.Storage.GetFreeSlots("Shelf1");` |
| `IsFull(cont)` | Checks whether the container is full | `if (Api.Storage.IsFull("Shelf1"))` |

### Api.Phone

| Method | Description | Example |
|---|---|---|
| `IsPhoneOpen()` | Checks whether the phone is open | `if (Api.Phone.IsPhoneOpen())` |
| `Open()` | Opens the phone | `Api.Phone.Open();` |
| `Close()` | Closes the phone | `Api.Phone.Close();` |
| `OpenApp(name)` | Opens an app | `Api.Phone.OpenApp("Messages");` |
| `GetAvailableApps()` | Names of all apps | `string[] a = Api.Phone.GetAvailableApps();` |
| `SendMessage(contact, txt)` | Sends a message to a contact | `Api.Phone.SendMessage("Jerry","Hi!");` |
| `GetMessages()` | All of the player's messages | `string[] m = Api.Phone.GetMessages();` |

### Api.Progression

| Method | Description | Example |
|---|---|---|
| `GetLevel()` | Current tier level (1–5 within the rank) | `int l = Api.Progression.GetLevel();` |
| `GetXP()` | Current XP within the tier | `int x = Api.Progression.GetXP();` |
| `AddXP(amount)` | Adds XP | `Api.Progression.AddXP(500);` |
| `GetRank()` | Current rank name (e.g. "Street_Rat") | `string r = Api.Progression.GetRank();` |
| `GetXPForNextLevel()` | XP required for the next tier level | `int n = Api.Progression.GetXPForNextLevel();` |

### Api.Debug

| Method | Description | Example |
|---|---|---|
| `Log(msg)` | Writes a debug message (DEBUG builds only) | `Api.Debug.Log("Mod loaded");` |
| `LogWarning(msg)` | Writes a warning to the log | `Api.Debug.LogWarning("Low energy");` |
| `LogError(msg)` | Writes an error to the log | `Api.Debug.LogError("Failed to load");` |
| `Assert(cond, msg)` | Logs an error if the condition is false | `Api.Debug.Assert(Api.Player.IsAlive(),"Dead");` |
| `DrawLine(x1..z2,r,g,b)` | Draws a world debug line (visible 1 s) | `Api.Debug.DrawLine(0,0,0,10,0,0,1,0,0);` |

---

## Events (Api.Events)

| Member | Description | Example |
|---|---|---|
| `OnPlayerArrested` | Player gets arrested | `Api.Events.OnPlayerArrested += () => …;` |
| `OnPlayerReleased` | Player released from jail | `Api.Events.OnPlayerReleased += () => …;` |
| `OnDayChanged` | New in-game day begins | `Api.Events.OnDayChanged += () => …;` |
| `OnPlayerDamaged` | Player takes damage (float) | `Api.Events.OnPlayerDamaged += dmg => …;` |
| `OnItemSold` | Item is sold (string id, int qty) | `Api.Events.OnItemSold += (id,q) => …;` |
| `OnSceneLoaded` | New scene loaded | `Api.Events.OnSceneLoaded += () => …;` |
| `ClearAll()` | Unregisters all event handlers | `Api.Events.ClearAll();` |

---

## Types (Api.Types)

```csharp
// Item qualities
enum ItemQuality { Poor, Standard, Premium, Heavenly }

// Item categories
enum ItemCategory { Product, Seed, Tool, Clothing, Weapon, Packaging, Misc }

// NPC relationships
enum Relationship { Neutral, Friend, CloseFriend, BestFriend }

// Quest states
enum QuestState { Active, Completed, Failed, Expired }

// Weather types
enum WeatherType { Clear, Cloudy, Rainy, Stormy }

// Police escalation
enum CrimeLevel { None, Investigating, Arresting, NonLethal, Lethal }

// Employee jobs
enum EmployeeType { Botanist, Chemist, Packager, Cleaner }

// NPC behaviours
enum BehaviourType { Idle, Patrol, Pursue, Flee, Consume, Search, Sleep }

// Crafting stations
enum CraftingStation { Chemistry, Mixing, Oven, Drying, Packing }

// Item info record: Api.Types.ItemInfo(string Id, string Name, ItemQuality Quality, float Value)

// Player snapshot: Api.Types.PlayerState(float Health, float Energy, float Cash, bool IsArrested)
```
