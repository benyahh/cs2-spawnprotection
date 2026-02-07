# CS2-SpawnProtection

A simple spawn protection plugin for CS2.

---

## Requirements

- CounterStrikeSharp
- Metamod:Source
- .NET 8

---

## How to use

1. Install CounterStrikeSharp and Metamod:Source
2. Download CS2-SpawnProtection
3. Upload the SpawnProtection folder to:

   addons/counterstrikesharp/plugins/

4. Restart server or use:

   css_plugins reload

---

## Features

- Spawn protection at round start
- Configurable protection time
- Center countdown message
- Configurable text and color
- Players hidden during protection
- PvP blocked
- Weapon desync fix

---

## Configure

Config file location:

addons/counterstrikesharp/configs/plugins/SpawnProtection/SpawnProtection.json


Example config:

```json
{
  "Enabled": true,
  "Seconds": 10,
  "CountdownText": "Spawn Protection {SECONDS} sec.",
  "EndText": "Spawn Protection is gone!",
  "UseColoredCenterText": true,
  "TextColorHex": "#FF0000",
  "ForceWeaponResyncOnEnd": true,
  "WeaponResyncPulses": 4,
  "WeaponResyncPulseInterval": 0.2
}
