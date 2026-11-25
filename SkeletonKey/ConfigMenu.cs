#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Boogle;

internal sealed class ConfigMenu
{
    private static readonly Dictionary<string, string> CommonContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        [ModConfig.JacketId] = "Jacket",
        ["578f87a3245977356274f2cb"] = "Duffle bag",
        ["578f8782245977354405a1e3"] = "Safe",
    };

    private readonly string path;
    private readonly List<LootRow> rows = new();
    private string? loadedText;
    private string status = "";
    private string keyUses = "";
    private string keycardUses = "";
    private string keyPrice = "";
    private string keycardPrice = "";
    private string bossKeyChance = "";
    private string bossKeycardChance = "";

    internal ConfigMenu(ConfigFile config, string gameRoot)
    {
        path = Path.Combine(gameRoot, "SPT_Runtime", "user", "mods", "Boogle-SkeletonKey", "config.json");
        config.Bind("Server Settings", "Local config.json", false, new ConfigDescription(
            "Edit Skeleton Key settings for the local SPT server. Restart SPT after saving.", null,
            new ConfigurationManagerAttributes { CustomDrawer = Draw, HideDefaultButton = true, HideSettingName = true }));
        Reload();
    }

    private void Draw(ConfigEntryBase _)
    {
        GUILayout.BeginVertical();
        GUILayout.Label("Local server settings. Restart SPT after saving.");

        if (loadedText is not null)
        {
            keyUses = Field("Key uses (0 = unlimited)", keyUses);
            keycardUses = Field("Keycard uses (0 = unlimited)", keycardUses);
            keyPrice = Field("Key price (roubles)", keyPrice);
            keycardPrice = Field("Keycard price (roubles)", keycardPrice);
            bossKeyChance = Field("Boss key chance (%)", bossKeyChance);
            bossKeycardChance = Field("Boss keycard chance (%)", bossKeycardChance);

            GUILayout.Space(8);
            GUILayout.Label("Container loot weights (0 = off, 1 = original weight)");
            DrawContainers();
        }

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        if (loadedText is not null && GUILayout.Button("Save server settings"))
        {
            Save();
        }
        if (GUILayout.Button("Reload from disk"))
        {
            Reload();
        }
        GUILayout.EndHorizontal();
        GUILayout.Label(status);
        GUILayout.EndVertical();
    }

    private void DrawContainers()
    {
        var removeIndex = -1;
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            GUILayout.BeginHorizontal();
            var name = CommonContainers.TryGetValue(row.Id.Trim(), out var knownName) ? knownName : "Container ID";
            GUILayout.Label(name, GUILayout.Width(90));
            row.Id = GUILayout.TextField(row.Id);
            if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
            {
                removeIndex = i;
            }
            GUILayout.EndHorizontal();
            row.Key = Field("Key weight", row.Key);
            row.Keycard = Field("Keycard weight", row.Keycard);
            GUILayout.Space(4);
        }

        if (removeIndex >= 0)
        {
            rows.RemoveAt(removeIndex);
        }

        GUILayout.BeginHorizontal();
        foreach (var container in CommonContainers)
        {
            if (GUILayout.Button($"+ {container.Value}"))
            {
                AddContainer(container.Key);
            }
        }
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Add custom container ID"))
        {
            AddContainer("");
        }
    }

    private void AddContainer(string id)
    {
        if (rows.Any(row => string.Equals(row.Id.Trim(), id, StringComparison.OrdinalIgnoreCase)))
        {
            status = "That container is already in the list.";
            return;
        }

        rows.Add(new LootRow(id, "0", "0"));
        status = "New container added. Set a weight and save to apply.";
    }

    private void Reload()
    {
        try
        {
            var json = File.ReadAllText(path);
            var config = JObject.Parse(json).ToObject<ModConfig>()
                ?? throw new InvalidDataException("config.json is empty.");
            var loot = config.GetValidatedSpawnWeights();

            keyUses = config.KeyUses.ToString(CultureInfo.InvariantCulture);
            keycardUses = config.KeycardUses.ToString(CultureInfo.InvariantCulture);
            keyPrice = config.KeyPrice.ToString(CultureInfo.InvariantCulture);
            keycardPrice = config.KeycardPrice.ToString(CultureInfo.InvariantCulture);
            bossKeyChance = config.BossKeyChance.ToString("R", CultureInfo.InvariantCulture);
            bossKeycardChance = config.BossKeycardChance.ToString("R", CultureInfo.InvariantCulture);
            rows.Clear();
            foreach (var entry in loot)
            {
                rows.Add(new LootRow(entry.Key, entry.Value.Key.ToString("R", CultureInfo.InvariantCulture),
                    entry.Value.Keycard.ToString("R", CultureInfo.InvariantCulture)));
            }
            loadedText = json;
            status = "Loaded config.json.";
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            loadedText = null;
            status = "Local config.json is missing. Start the local server, then reload.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            loadedText = null;
            status = $"Could not read config.json. {ex.Message}";
        }
    }

    private void Save()
    {
        try
        {
            var weights = new Dictionary<string, LootWeights>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var id = row.Id.Trim();
                if (weights.ContainsKey(id))
                {
                    throw new InvalidDataException($"Container {id} is listed more than once.");
                }
                weights.Add(id, new LootWeights { Key = ParseNumber(row.Key, "Key weight"), Keycard = ParseNumber(row.Keycard, "Keycard weight") });
            }

            var config = new ModConfig
            {
                KeyUses = ParseInteger(keyUses, "Key uses"),
                KeycardUses = ParseInteger(keycardUses, "Keycard uses"),
                KeyPrice = ParseInteger(keyPrice, "Key price"),
                KeycardPrice = ParseInteger(keycardPrice, "Keycard price"),
                BossKeyChance = ParseNumber(bossKeyChance, "Boss key chance"),
                BossKeycardChance = ParseNumber(bossKeycardChance, "Boss keycard chance"),
                SpawnWeights = weights,
            };
            var spawnWeights = config.GetValidatedSpawnWeights();
            var json = JObject.Parse(loadedText!);
            json.Remove(nameof(ModConfig.Containers));
            json[nameof(ModConfig.KeyUses)] = config.KeyUses;
            json[nameof(ModConfig.KeycardUses)] = config.KeycardUses;
            json[nameof(ModConfig.KeyPrice)] = config.KeyPrice;
            json[nameof(ModConfig.KeycardPrice)] = config.KeycardPrice;
            json[nameof(ModConfig.BossKeyChance)] = config.BossKeyChance;
            json[nameof(ModConfig.BossKeycardChance)] = config.BossKeycardChance;
            json[nameof(ModConfig.SpawnWeights)] = JObject.FromObject(spawnWeights);

            var newText = json.ToString(Formatting.Indented) + Environment.NewLine;
            var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(tempPath, newText);
                if (!string.Equals(File.ReadAllText(path), loadedText, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("config.json changed on disk. Reload it before saving.");
                }
                File.Replace(tempPath, path, null);
            }
            finally
            {
                File.Delete(tempPath);
            }
            loadedText = newText;
            status = "Saved config.json. Restart SPT to apply the changes.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            status = $"Settings were not saved. {ex.Message}";
        }
    }

    private static string Field(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(160));
        var updated = GUILayout.TextField(value);
        GUILayout.EndHorizontal();
        return updated;
    }

    private static int ParseInteger(string text, string label) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value : throw new InvalidDataException($"{label} must be a whole number.");

    private static float ParseNumber(string text, string label) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value : throw new InvalidDataException($"{label} must be a number. Use a decimal point for fractions.");

    private sealed class LootRow(string id, string key, string keycard)
    {
        internal string Id = id;
        internal string Key = key;
        internal string Keycard = keycard;
    }

#pragma warning disable CS0649
    private sealed class ConfigurationManagerAttributes
    {
        public Action<ConfigEntryBase>? CustomDrawer;
        public bool? HideDefaultButton;
        public bool? HideSettingName;
    }
#pragma warning restore CS0649
}
