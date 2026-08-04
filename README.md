# SillyValuables Fix

This mod is a patch for the original **SillyValuables** mod mod for R.E.P.O game.

I didn't see any way available to fix it, so I made one with help of gemini. (I spent my whole afternoon on it -_-)
Yes, the image was made in paint =w=

## What does this fix do?

This mod fixes the following crash/error log caused by the original `SillyValuables` mod calling an obsolete method:

```text
[Error  : Unity Log] MissingMethodException: Method not found: void .Sound.PlayLoop(bool,single,single,single)
Stack trace:
CustomLogHandler:LogException(Exception, Object) (at SillyValuables/harmony.cs:67)
```

* **Console/Chat Spam Fix:** Eliminates the `MissingMethodException` thrown when activating grenades.
* **Audio System Fix:** Replaces obsolete audio calls (`PlayLoop`) with a native 3D audio emitter synchronized with the game's original mine beep sound (`item explosive mine warning beeps`).
* **Animation and Physics Sync:** Restores pin logic, throw timer, and impact explosion functionality.

## Requirements

* **BepInEx**
* **SillyValuables** (automatically downloaded when installing this fix)

## Compatibility Notice

This patch was created specifically for **SillyValuables v7.0.7**. 

If the original mod author updates **SillyValuables** and fixes the grenade issue natively, this fix mod may no longer be required.

## AI Disclosure

This mod was partially or fully created with the assistance of Generative AI (Google Gemini).

## Installation

Recommended to install via **Thunderstore Mod Manager** or **r2modman**. For manual installation, place the `SillyValuablesFix.dll` file into your `BepInEx/plugins` folder.
