#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace Boogle;

internal sealed class ModConfig
{
    internal const string JacketId = "578f8778245977358849a9b5";
    internal const float DefaultBossKeyChance = 1;
    internal const float DefaultBossKeycardChance = 0.5f;

    public int KeyPrice { get; set; }
    public int KeycardPrice { get; set; }
    public int KeyUses { get; set; } = -1;
    public int KeycardUses { get; set; } = -1;
    public float BossKeyChance { get; set; } = DefaultBossKeyChance;
    public float BossKeycardChance { get; set; } = DefaultBossKeycardChance;
    public Dictionary<string, float>? Containers { get; set; }
    public Dictionary<string, LootWeights>? SpawnWeights { get; set; }

    internal static ModConfig CreateDefault() => new()
    {
        KeyPrice = 1_500_000,
        KeycardPrice = 2_500_000,
        KeyUses = 10,
        KeycardUses = 10,
        SpawnWeights = new() { [JacketId] = new() { Key = 100, Keycard = 50 } },
    };

    internal Dictionary<string, LootWeights> GetValidatedSpawnWeights()
    {
        if (KeyUses < 0 || KeycardUses < 0 || KeyPrice <= 0 || KeycardPrice <= 0)
        {
            throw new InvalidDataException("Uses must be nonnegative and prices must be positive.");
        }

        if (!IsWeight(BossKeyChance) || BossKeyChance > 100 || !IsWeight(BossKeycardChance) || BossKeycardChance > 100)
        {
            throw new InvalidDataException("Boss chances must be between 0 and 100 percent.");
        }

        var result = new Dictionary<string, LootWeights>(StringComparer.OrdinalIgnoreCase);
        if (SpawnWeights is not null)
        {
            foreach (var entry in SpawnWeights)
            {
                AddWeights(result, entry.Key, entry.Value);
            }
        }
        else if (Containers is not null)
        {
            foreach (var entry in Containers)
            {
                AddWeights(result, entry.Key, new LootWeights { Key = entry.Value, Keycard = entry.Value });
            }
        }
        else
        {
            throw new InvalidDataException("Add a SpawnWeights or Containers object to config.json.");
        }

        return result;
    }

    private static void AddWeights(Dictionary<string, LootWeights> settings, string id, LootWeights? weights)
    {
        if (id.Length != 24 || !IsHexadecimal(id))
        {
            throw new InvalidDataException($"Container ID '{id}' must contain 24 hexadecimal characters.");
        }

        if (weights is null || !IsWeight(weights.Key) || !IsWeight(weights.Keycard))
        {
            throw new InvalidDataException($"Container {id} needs nonnegative, finite loot weights.");
        }

        if (settings.ContainsKey(id))
        {
            throw new InvalidDataException($"Container {id} is listed more than once.");
        }

        settings.Add(id.ToLowerInvariant(), weights);
    }

    private static bool IsWeight(float value) => value >= 0 && !float.IsInfinity(value);

    private static bool IsHexadecimal(string id)
    {
        foreach (var ch in id)
        {
            if (ch is not (>= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed class LootWeights
{
    public float Key { get; set; }
    public float Keycard { get; set; }
}
