# SpawnProtection for CS2

Spawn protection plugin for CounterStrike 2 using CounterStrikeSharp.

## Features
- Spawn protection at round start
- Configurable protection time (seconds)
- Configurable countdown text
- Configurable text color
- Players hidden during protection
- PvP blocked during protection
- Center countdown message

## Requirements
- CounterStrikeSharp
- .NET 8

## Installation

1. Download SpawnProtection.dll
2. Copy it to:
   addons/counterstrikesharp/plugins/SpawnProtection/
3. Restart server or run: css_plugins reload

## Configuration

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
