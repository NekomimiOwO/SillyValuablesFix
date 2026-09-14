(I can't believe I just realized I forgot to thank the original mod creator... next time I update I have to remember to include it there.)

# SillyValuables Fix

This mod is a patch for the original **SillyValuables** mod.

I didn't see any way available to fix it, so I made one with help of gemini. (I spent my whole afternoon on it -_-)
I'm lazy so the image to this was made in paint =w=

## What does this fix do?

This mod fixes the following crash/error log caused by the original `SillyValuables` mod calling an obsolete method:

```text
[Error  : Unity Log] MissingMethodException: Method not found: void .Sound.PlayLoop(bool,single,single,single)
Stack trace:
CustomLogHandler:LogException(Exception, Object) (at SillyValuables/harmony.cs:67)
```

Now this mod also tries to fix these error messages for some itens (August 18th, 2026):

```text
[Error  :UnityExplorer] [Unity] Observed scripts have to implement IPunObservable. Item Comic Large Pencil(Clone) (ItemMelee) does not. It is Type: ItemMelee
[Error  : Unity Log] Observed scripts have to implement IPunObservable. Item Summit Shaper(Clone) (ItemMelee) does not. It is Type: ItemMelee
[Error  : Unity Log] Observed scripts have to implement IPunObservable. Module - Manor - DE - 1 - Small Bedroom(Clone) (Module) does not. It is Type: Module
```

*(Note:some maps rooms and these items do not actually crash or break the game. They just have unused components mistakenly left in their network observation list. The mod simply removes what isn't being used to prevent the console spam.)*

New weapons Stuck in Head Fix (September 14, 2026)

### Features & Fixes

* **Network Lag & Stuttering Fix:** Fix log spam caused by the `IPunObservable` error. It safely removes improperly configured scripts (`ItemMelee`, `Module`) from Photon's network observation list exactly when they spawn, keeping multiplayer RPCs and physical sync 100% intact.
* **Console/Chat Spam Fix:** Eliminates the `MissingMethodException` thrown when activating grenades by unpatching broken hooks left in the memory.
* **Audio System Fix:** Replaces obsolete audio calls (`PlayLoop`) with a native 3D audio emitter synchronized with the game's original mine beep sound (`item explosive mine warning beeps`).
* **Animation and Physics Sync:** Restores pin logic, throw timer, and impact explosion functionality for grenades.
* **Guns Stuck in Head Fix:** Golden Deagle, Healing Gun, Freeze Gun and Rainbow Pistol no longer snap into the player's head when grabbed (their hold distance was 0, now 0.8 like every other gun).

## Requirements

* **BepInEx**
* **SillyValuables** (automatically downloaded when installing this fix)

## Compatibility Notice

This patch was created specifically for **SillyValuables v7.0.7**. 

If the original mod author updates **SillyValuables** and fixes the grenade issue natively, this fix mod may no longer be required.

## Credits & Thanks

* thanks to [**sweetbenefituz**](https://github.com/sweetbenefituz) for contributing the pull request that fixes the custom guns getting stuck in the player's head /ᐠ - ˕ -マ

## Example for broken item fix:

![SillyValuables Fix](https://raw.githubusercontent.com/NekomimiOwO/SillyValuablesFix/main/SillyValuablesFix.png)

## Example of photon fix

![photon fix](https://raw.githubusercontent.com/NekomimiOwO/SillyValuablesFix/refs/heads/main/image.png)

## Example of holding a gun

### Before:

![Before](https://raw.githubusercontent.com/NekomimiOwO/SillyValuablesFix/refs/heads/main/before.jpg)

### After:

![After](https://raw.githubusercontent.com/NekomimiOwO/SillyValuablesFix/refs/heads/main/after.jpg)

## AI Disclosure

This mod was partially or fully created with the assistance of Generative AI (Google Gemini).

## License

This project is licensed under the MIT License.

## Contact

You can contact me at discord: nekomimiowo or at the github of this mod: https://github.com/NekomimiOwO/SillyValuablesFix

## Installation

Recommended to install via **Thunderstore Mod Manager** or **r2modman**. For manual installation, place the `SillyValuablesFix.dll` file into your `BepInEx/plugins` folder.
