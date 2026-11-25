using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace Boogle
{
    public sealed class KeycardDoorUnlockPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            UnlockComparison.Target(typeof(KeycardDoor));

        [PatchTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            UnlockComparison.Replace(instructions, AccessTools.Method(typeof(KeycardDoorUnlockPatch), nameof(Matches)));

        private static bool Matches(string keyId, string requiredId) =>
            keyId == requiredId || keyId == ItemIds.Keycard;
    }
}
