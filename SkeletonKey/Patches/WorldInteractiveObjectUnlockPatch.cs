using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Boogle
{
    public sealed class WorldInteractiveObjectUnlockPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            UnlockComparison.Target(typeof(WorldInteractiveObject));

        [PatchTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            UnlockComparison.Replace(instructions, AccessTools.Method(typeof(WorldInteractiveObjectUnlockPatch), nameof(Matches)));

        private static bool Matches(string keyId, string requiredId) =>
            keyId == requiredId || (keyId == ItemIds.Key && !string.IsNullOrEmpty(requiredId));
    }

    internal static class UnlockComparison
    {
        internal static MethodBase Target(Type doorType) =>
            AccessTools.DeclaredMethod(doorType, nameof(WorldInteractiveObject.UnlockOperation),
                new[] { typeof(KeyComponent), typeof(Player), typeof(WorldInteractiveObject) });

        internal static IEnumerable<CodeInstruction> Replace(IEnumerable<CodeInstruction> instructions, MethodInfo matches)
        {
            var keyId = AccessTools.Field(typeof(WorldInteractiveObject), nameof(WorldInteractiveObject.KeyId));
            var equality = AccessTools.Method(typeof(string), "op_Equality", new[] { typeof(string), typeof(string) });
            var matcher = new CodeMatcher(instructions).MatchForward(true,
                new CodeMatch(OpCodes.Ldfld, keyId),
                new CodeMatch(OpCodes.Call, equality));

            if (!matcher.IsValid)
            {
                throw new InvalidOperationException("The native door key comparison could not be found.");
            }

            // Keep the original branches, secondary key checks and durability handling.
            matcher.Operand = matches;
            return matcher.InstructionEnumeration();
        }
    }
}
