using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Boogle
{
    public sealed class SkeletonKeyActionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(InteractionContextHelper), nameof(InteractionContextHelper.TryAddUnlock));

        [PatchPostfix]
        private static void Postfix(GamePlayerOwner owner, WorldInteractiveObject worldInteractiveObject,
            List<InteractionAction> actions, ref IResult __result)
        {
            var door = worldInteractiveObject;
            if (door is KeycardDoor || string.IsNullOrEmpty(door.KeyId))
            {
                return;
            }

            var key = FindSkeletonKey(owner);
            if (key == null)
            {
                return;
            }

            var skillResult = InteractionContextHelper.CanUnlockBySkill(owner, door);
            if (skillResult.Failed)
            {
                __result = skillResult;
                return;
            }

            actions.Add(new InteractionAction
            {
                Name = "Use Skeleton Key",
                Disabled = !door.Operatable,
                Action = () => Unlock(owner.Player, door, key)
            });
            __result = new SuccessfulResult();
        }

        internal static KeyComponent FindSkeletonKey(PlayerOwner owner) =>
            owner.Player.InventoryController.Inventory.Equipment.GetItemComponentsInChildren<KeyComponent>(false)
                .FirstOrDefault(key => key.Template.KeyId == ItemIds.Key);

        private static void Unlock(Player player, WorldInteractiveObject door, KeyComponent key)
        {
            if (door.DoorState != EDoorState.Locked || !door.Operatable)
            {
                return;
            }

            player.MovementContext.ResetCanUsePropState();
            var operation = door.UnlockOperation(key, player, door);
            if (operation.Failed)
            {
                return;
            }

            var result = operation.Value;
            var inventory = player.InventoryController;
            result.RaiseEvents(inventory, CommandStatus.Begin);
            player.StartInteraction(door, result, () => result.RaiseEvents(inventory, CommandStatus.Succeed));
        }
    }

    public sealed class SwitchGetKeyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(PlayerOwner), nameof(PlayerOwner.GetKey));

        [PatchPostfix]
        private static void Postfix(PlayerOwner __instance, WorldInteractiveObject worldInteractiveObject,
            ref KeyComponent __result)
        {
            if (__result == null && worldInteractiveObject is Switch && !string.IsNullOrEmpty(worldInteractiveObject.KeyId))
            {
                __result = SkeletonKeyActionPatch.FindSkeletonKey(__instance);
            }
        }
    }
}
