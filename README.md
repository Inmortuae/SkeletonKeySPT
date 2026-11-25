# Skeleton Key for Single Player Tushonka (SPT) 4.1.6

Skeleton Key adds two rare items to jacket loot. Use the **Skeleton Key** on mechanical locks and the **TerraGroup Labs active master keycard** at keycard readers. The keycard uses the Labs master-keycard model supplied by WTT Content Backport.

## Install

1. Close the SPT client and server.
2. Install [WTT Content Backport 2.0.7 or a compatible newer version](https://sp-mod.com/mod/2512/wtt-content-backport) and its required WTT CommonLib release.
3. Extract the release ZIP into your SPT game folder. The archive contains the `BepInEx` and `SPT_Runtime` folders.
4. Start SPT. Look for both items in jackets. At a mechanical door, select **Use Skeleton Key**. At a locked card reader, select **Active Master** from the keycard choices.

The original door key can still be selected through the standard **Unlock** action. The mod keeps the original item IDs and follows the game's normal door restrictions and key durability rules.

## Configuration

Edit `SPT_Runtime/user/mods/Boogle-SkeletonKey/config.json` with the server closed. Both items start with 10 uses. Set an item's use count to `0` for unlimited use. `KeyPrice` and `KeycardPrice` set reference prices in roubles. `Containers` maps container template IDs to relative loot weights. A weight of `0` disables the mod's spawn in that container.

## Build

Use the .NET 10 SDK and the .NET Framework 4.7.2 targeting pack on Windows. Build against an SPT 4.1.6 installation.

```powershell
dotnet build SkeletonKey.sln -c Release -p:SptRoot=C:\SPT
```

The client DLL belongs in `BepInEx/plugins/Boogle-SkeletonKey/`. The server DLL and `config.json` belong in `SPT_Runtime/user/mods/Boogle-SkeletonKey/`.

## Credits

Boogle created [Skeleton Key](https://github.com/TheBoogle/SkeletonKeySPT). Original contributors include JankyTheClown and RaiRaiTheRaichu. Inmortuae ported it to SPT 4.1.6 with Boogle’s permission. Thanks to the WTT team for making the master-keycard bundle available through [Content Backport](https://sp-mod.com/mod/2512/wtt-content-backport). This project's code is available under the MIT license.
