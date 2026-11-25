using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;
using Path = System.IO.Path;
using Version = SemanticVersioning.Version;
using VersionRange = SemanticVersioning.Range;

[assembly: AssemblyTitle("Skeleton Key Server")]
[assembly: AssemblyVersion(Boogle.ItemIds.Version)]
[assembly: AssemblyFileVersion(Boogle.ItemIds.Version)]

namespace Boogle;

public sealed record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.boogle.skeletonkey";
    public string Name { get; init; } = "Skeleton Key";
    public string Author { get; init; } = "Boogle";
    public List<string>? Contributors { get; init; } = ["Inmortuae", "JankyTheClown", "RaiRaiTheRaichu"];
    public Version Version { get; init; } = new(ItemIds.Version);
    public VersionRange SptVersion { get; init; } = new("4.1.6");
    public bool HasPrepatcher { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, VersionRange>? ModDependencies { get; init; } =
        new() { ["com.wtt.contentbackport"] = new(">=2.0.7") };
    public string? Url { get; init; } = "https://github.com/Inmortuae/SkeletonKeySPT";
    public string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.HandbookCallbacks - 1)]
public sealed class SkeletonKeyMod(CustomItemService items, LocationTable locations, BossLoot bossLoot) : IOnLoad
{
    private const string MasterCardBundle =
        "assets/content/items/spec/item_keycard_lab/item_keycard_quest_master_lab.bundle";
    private static readonly JsonSerializerOptions ConfigJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(SkeletonKeyMod).Assembly;
        var path = Path.Combine(Path.GetDirectoryName(assembly.Location)!, "config.json");
        var config = LoadConfig(path);
        var spawnWeights = config.GetValidatedSpawnWeights();

        RegisterItem(ItemIds.Key, "5780cf7f2459777de4559322", "5c99f98d86f7745c314214b3",
            "5c518ec986f7743b68682ce2", "skeleton_key", "yellow",
            "assets/content/items/spec/keys/item_key_11.bundle", config.KeyUses, config.KeyPrice,
            "Skeleton Key", "Skeleton", "A rare key that opens mechanical locks.");
        RegisterItem(ItemIds.Keycard, "5c94bbff86f7747ee735c08f", "5c164d2286f774194c5e69fa",
            "5c518ed586f774119a772aee", "skeleton_priority_keycard", "violet",
            MasterCardBundle, config.KeycardUses, config.KeycardPrice,
            "TerraGroup Labs active master keycard", "Active Master",
            "An active master keycard that grants access at compatible keycard readers.");

        foreach (var location in locations.GetDictionary().Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var staticLoot = location?.StaticLoot;
            if (staticLoot is null)
            {
                continue;
            }

            staticLoot.AddTransformer(containers =>
            {
                foreach (var (id, weights) in spawnWeights)
                {
                    MongoId containerId = id;
                    if ((weights.Key == 0 && weights.Keycard == 0) || containers is null ||
                        !containers.TryGetValue(containerId, out var loot) || loot is null)
                    {
                        continue;
                    }

                    var distribution = loot.ItemDistribution?.ToList() ?? [];
                    AddLoot(distribution, ItemIds.Key, weights.Key);
                    AddLoot(distribution, ItemIds.Keycard, weights.Keycard);
                    containers[containerId] = loot with { ItemDistribution = distribution };
                }

                return containers;
            });
        }

        bossLoot.Install(config);
        return Task.CompletedTask;
    }

    private static void AddLoot(List<ItemDistribution> distribution, MongoId item, float weight)
    {
        if (weight > 0 && !distribution.Any(entry => entry.Tpl == item))
        {
            distribution.Add(new ItemDistribution { Tpl = item, RelativeProbability = weight });
        }
    }

    private static ModConfig LoadConfig(string path)
    {
        if (!File.Exists(path))
        {
            var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
            try
            {
                var defaults = JsonSerializer.Serialize(ModConfig.CreateDefault(), ConfigJson);
                File.WriteAllText(tempPath, defaults + Environment.NewLine);
                File.Move(tempPath, path);
            }
            catch (IOException) when (File.Exists(path))
            {
                // Another server instance created the configuration first.
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        return JsonSerializer.Deserialize<ModConfig>(File.ReadAllText(path), ConfigJson)
            ?? throw new InvalidDataException("Skeleton Key config.json is empty.");
    }

    private void RegisterItem(MongoId id, MongoId template, MongoId parent, string handbookCategory,
        string internalName, string color, string bundle, int uses, int price,
        string name, string shortName, string description)
    {
        var result = items.CreateItemFromClone(new NewItemFromCloneDetails
        {
            NewId = id,
            NewItemName = internalName,
            ParentId = parent,
            ItemTplToClone = template,
            OverrideProperties = new TemplateItemProperties
            {
                MaximumNumberOfUsage = uses,
                BackgroundColor = color,
                Prefab = new Prefab { Path = bundle, Rcid = "" }
            },
            AddToHandbook = true,
            AddToFleaPriceDb = true,
            HandbookParentId = handbookCategory,
            HandbookPriceRoubles = price,
            FleaPriceRoubles = price,
            Locales = new() { ["en"] = new() { Name = name, ShortName = shortName, Description = description } }
        }, typeof(SkeletonKeyMod).Assembly);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Skeleton Key could not register {name}: {string.Join(", ", result.Errors ?? [])}");
        }
    }
}
