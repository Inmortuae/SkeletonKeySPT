# Skeleton Key for Single Player Tushonka (SPT) 4.1.6

Skeleton Key adds two rare items to jacket and boss loot. Use the **Skeleton Key** on mechanical locks and the **TerraGroup Labs active master keycard** at keycard readers. The keycard uses the Labs master-keycard model supplied by WTT Content Backport.

## Install

1. Close the SPT client and server.
2. Install [WTT Content Backport 2.0.7 or a compatible newer version](https://sp-mod.com/mod/2512/wtt-content-backport) and its required WTT CommonLib release.
3. Extract the release ZIP into your SPT game folder. The archive contains the `BepInEx` and `SPT_Runtime` folders.
4. Start SPT. Look for both items in jackets. At a mechanical door, select **Use Skeleton Key**. At a locked card reader, select **Active Master** from the keycard choices.

The original door key can still be selected through the standard **Unlock** action. The mod keeps the original item IDs and follows the game's normal door restrictions and key durability rules.

## Configuration

In SPT, press **F12** and open **Skeleton Key → Server Settings**. Edit the settings, select **Save server settings**, then restart SPT. With the server on another machine, edit the file on the server itself and restart it.

The server creates `SPT_Runtime/user/mods/Boogle-SkeletonKey/config.json` on first start. You can edit it directly with the server closed or use F12 to edit the same file. Use **Reload from disk** to pick up manual changes. Both items start with 10 uses. Set uses to `0` for unlimited use. Prices are in roubles.

- **Container loot.** Set an independent `Key` and `Keycard` weight in `SpawnWeights` for each container template ID. F12 has shortcuts for jackets, duffle bags, and safes, plus a custom ID field. New installs use jacket weights of `100` for the key and `50` for the card. Weights compete with the container's other loot and are relative, so the odds vary by location. At `0`, Skeleton Key skips adding that item to the container pool.
- **Boss loot.** Each eligible boss gets independent rolls at generation, with default chances of `1%` for the key and `0.5%` for the card. A successful roll also requires room in a lootable pocket, rig, or backpack. Set either chance to `0` to disable it. SPT identifies which bots count as bosses.
- **Upgrading.** An existing `Containers` entry still applies its original weight to both items. The first save from F12 writes those weights separately under `SpawnWeights`. The installer leaves `config.json` in place. See [the example config](SkeletonKey.Server/config.example.json) for all fields.

## Build

Use the .NET 10 SDK and the .NET Framework 4.7.2 targeting pack on Windows. Build against an SPT 4.1.6 installation.

```powershell
dotnet build SkeletonKey.sln -c Release -p:SptRoot=C:\SPT
```

The client DLL belongs in `BepInEx/plugins/Boogle-SkeletonKey/`. The server DLL belongs in `SPT_Runtime/user/mods/Boogle-SkeletonKey/`, where it creates `config.json` if needed.

## Credits

Boogle created [Skeleton Key](https://github.com/TheBoogle/SkeletonKeySPT). Original contributors include JankyTheClown and RaiRaiTheRaichu. Inmortuae ported it to SPT 4.1.6 with Boogle’s permission. Thanks to the WTT team for making the master-keycard bundle available through [Content Backport](https://sp-mod.com/mod/2512/wtt-content-backport). This project's code is available under the MIT license.
