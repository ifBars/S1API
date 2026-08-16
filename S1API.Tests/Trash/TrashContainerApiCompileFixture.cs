using System;
using System.Collections.Generic;
using S1API.Trash;
using UnityEngine;

namespace S1API.Tests.Trash;

internal static class TrashContainerApiCompileFixture
{
    internal static void ReadAndSubscribe(GameObject gameObject, TrashContainer container)
    {
        TrashContainer? existing = TrashContainer.FromGameObject(gameObject);
        TrashContainer[] containers = TrashContainer.FindInScene(includeInactive: true);
        GameObject? owner = container.GameObject;
        int capacity = container.Capacity;
        int level = container.Level;
        float normalizedLevel = container.NormalizedLevel;
        IReadOnlyList<TrashContentEntry> contents = container.Contents;
        bool canBeBagged = container.CanBeBagged;

        Action<string> trashAdded = _ => { };
        Action levelChanged = () => { };
        container.OnTrashAdded += trashAdded;
        container.OnTrashLevelChanged += levelChanged;
        container.OnTrashAdded -= trashAdded;
        container.OnTrashLevelChanged -= levelChanged;

        bool bagged = container.TryBagTrash();

        _ = existing;
        _ = containers;
        _ = owner;
        _ = capacity;
        _ = level;
        _ = normalizedLevel;
        _ = contents;
        _ = canBeBagged;
        _ = bagged;
    }
}
