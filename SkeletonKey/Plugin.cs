using BepInEx;

namespace Boogle
{
    [BepInPlugin("com.boogle.skeletonkey", "Skeleton Key", ItemIds.Version)]
    public sealed class Skeletonkey : BaseUnityPlugin
    {
        private void Awake()
        {
            new WorldInteractiveObjectUnlockPatch().Enable();
            new KeycardDoorUnlockPatch().Enable();
            new SkeletonKeyActionPatch().Enable();
            new SwitchGetKeyPatch().Enable();
            Logger.LogInfo("Skeleton Key interaction patches loaded.");
        }
    }
}
