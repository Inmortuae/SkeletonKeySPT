using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.Loot;
using SPTarkov.Server.Core.Helpers.Bot;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace Boogle;

[Injectable(InjectionType.Singleton)]
public sealed class BossLoot(
    BotHelper botHelper,
    BotGeneratorHelper botGeneratorHelper,
    ItemHelper itemHelper,
    BotConfig botConfig,
    ISptLogger<BossLoot> logger)
{
    private static readonly HashSet<EquipmentSlots> LootSlots =
        [EquipmentSlots.Pockets, EquipmentSlots.TacticalVest, EquipmentSlots.Backpack];

    private static BossLoot? active;
    private float keyChance;
    private float keycardChance;

    internal void Install(ModConfig config)
    {
        keyChance = config.BossKeyChance;
        keycardChance = config.BossKeycardChance;
        if (keyChance == 0 && keycardChance == 0)
        {
            return;
        }

        active = this;
        var target = AccessTools.Method(typeof(BotLootGenerator), nameof(BotLootGenerator.GenerateLoot));
        new Harmony("com.boogle.skeletonkey.bossloot").Patch(
            target, postfix: new HarmonyMethod(typeof(BossLoot), nameof(AfterLootGenerated)));
    }

    private static void AfterLootGenerated(
        MongoId botId, BotGenerationDetails botGenerationDetails, BotBaseInventory botInventory)
    {
        active?.AddBossLoot(botId, botGenerationDetails, botInventory);
    }

    private void AddBossLoot(MongoId botId, BotGenerationDetails details, BotBaseInventory inventory)
    {
        if (details.IsPmc || details.IsPlayerScav || inventory.Items is null ||
            !botHelper.IsBotBoss(details.RoleLowercase) || botConfig.DisableLootOnBotTypes.Contains(details.RoleLowercase))
        {
            return;
        }

        TryAddItem(botId, details.RoleLowercase, inventory, ItemIds.Key, keyChance);
        TryAddItem(botId, details.RoleLowercase, inventory, ItemIds.Keycard, keycardChance);
    }

    private void TryAddItem(MongoId botId, string role, BotBaseInventory inventory, MongoId templateId, float chance)
    {
        if (chance <= 0 || Random.Shared.NextDouble() * 100 >= chance ||
            inventory.Items!.Any(item => item.Template == templateId))
        {
            return;
        }

        try
        {
            var (found, template) = itemHelper.GetItem(templateId);
            if (!found || template is null)
            {
                return;
            }

            var item = new Item
            {
                Id = new MongoId(),
                Template = templateId,
                Upd = botGeneratorHelper.GenerateExtraPropertiesForItem(template, role, true),
            };
            botGeneratorHelper.AddItemWithChildrenToEquipmentSlot(
                botId, LootSlots, item.Id, templateId, [item], inventory);
        }
        catch (Exception ex)
        {
            logger.Warning($"Skeleton Key could not add boss loot for {role}: {ex.Message}");
        }
    }
}
