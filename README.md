# CS2-SpawnProtection

A simple spawn protection plugin for CS2.

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)

## How to use

1. Install [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) and [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)
2. Download [cs2-SpawnProtection](https://github.com/benyahh/cs2-spawnprotection/releases/download/1.4.3/cs2-SpawnProtection.zip)
3. Unzip the archive and upload `SpawnProtection` folder in the `plugins` folder.

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

addons/counterstrikesharp/configs/plugins/SpawnProtection/SpawnProtection.json

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
