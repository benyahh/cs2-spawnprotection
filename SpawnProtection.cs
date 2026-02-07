#nullable enable

using System;
using System.Reflection;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Timers;

namespace SpawnProtection;

public sealed class SpawnProtectionConfig : BasePluginConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("Seconds")]
    public int Seconds { get; set; } = 10;

    // Use {SECONDS}
    [JsonPropertyName("CountdownText")]
    public string CountdownText { get; set; } = "Spawn Protection {SECONDS} sec.";

    [JsonPropertyName("EndText")]
    public string EndText { get; set; } = "Spawn Protection is gone!";

    // Text color only (no background/box styling)
    [JsonPropertyName("UseColoredCenterText")]
    public bool UseColoredCenterText { get; set; } = true;

    [JsonPropertyName("TextColorHex")]
    public string TextColorHex { get; set; } = "#FF0000"; // red

    // Weapon resync pulses after unhide
    [JsonPropertyName("ForceWeaponResyncOnEnd")]
    public bool ForceWeaponResyncOnEnd { get; set; } = true;

    [JsonPropertyName("WeaponResyncPulses")]
    public int WeaponResyncPulses { get; set; } = 4;

    [JsonPropertyName("WeaponResyncPulseInterval")]
    public float WeaponResyncPulseInterval { get; set; } = 0.20f;

    // ❗ NINCS ConfigVersion itt (BasePluginConfig-ban már van)
}

[MinimumApiVersion(80)]
public sealed class SpawnProtectionPlugin : BasePlugin, IPluginConfig<SpawnProtectionConfig>
{
    public override string ModuleName => "SpawnProtection";
    public override string ModuleVersion => "1.4.3";
    public override string ModuleAuthor => "benyahh";

    public SpawnProtectionConfig Config { get; set; } = new();

    private bool _active;
    private int _secondsLeft;
    private Timer? _countdownTimer;

    // Extension method cache: static void PrintToCenterHtml(this CCSPlayerController, string)
    private static MethodInfo? _printToCenterHtmlExt;

    public void OnConfigParsed(SpawnProtectionConfig config)
    {
        if (config.Seconds < 1) config.Seconds = 1;

        if (string.IsNullOrWhiteSpace(config.CountdownText))
            config.CountdownText = "Spawn Protection {SECONDS} sec.";
        if (string.IsNullOrWhiteSpace(config.EndText))
            config.EndText = "Spawn Protection is gone!";

        if (string.IsNullOrWhiteSpace(config.TextColorHex))
            config.TextColorHex = "#FF0000";

        if (config.WeaponResyncPulses < 0) config.WeaponResyncPulses = 0;
        if (config.WeaponResyncPulses > 12) config.WeaponResyncPulses = 12;

        if (config.WeaponResyncPulseInterval < 0.05f) config.WeaponResyncPulseInterval = 0.05f;
        if (config.WeaponResyncPulseInterval > 2.0f) config.WeaponResyncPulseInterval = 2.0f;

        Config = config;
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Post);

        RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        RegisterListener<Listeners.OnEntityTakeDamagePre>(OnEntityTakeDamagePre);

        _printToCenterHtmlExt ??= FindPrintToCenterHtmlExtension();
    }

    public override void Unload(bool hotReload)
    {
        StopProtection(silent: true);
    }

    // -------------------------
    // Round flow
    // -------------------------
    private HookResult OnRoundStart(EventRoundStart ev, GameEventInfo info)
    {
        if (!Config.Enabled)
            return HookResult.Continue;

        StartProtection(Config.Seconds);
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd ev, GameEventInfo info)
    {
        StopProtection(silent: true);
        return HookResult.Continue;
    }

    private void StartProtection(int seconds)
    {
        StopProtection(silent: true);

        _active = true;
        _secondsLeft = seconds;

        BroadcastCenter(FormatCountdown(_secondsLeft));

        _countdownTimer = AddTimer(1.0f, () =>
        {
            if (!_active) return;

            _secondsLeft--;

            if (_secondsLeft > 0)
            {
                BroadcastCenter(FormatCountdown(_secondsLeft));
                return;
            }

            StopProtection(silent: false);
        }, TimerFlags.REPEAT);
    }

    private void StopProtection(bool silent)
    {
        _active = false;
        _secondsLeft = 0;

        if (_countdownTimer != null)
        {
            _countdownTimer.Kill();
            _countdownTimer = null;
        }

        if (!silent && Config.Enabled)
        {
            BroadcastCenter(Config.EndText);

            if (Config.ForceWeaponResyncOnEnd && Config.WeaponResyncPulses > 0)
            {
                for (int i = 0; i < Config.WeaponResyncPulses; i++)
                {
                    float delay = (i + 1) * Config.WeaponResyncPulseInterval;
                    AddTimer(delay, WeaponResyncPulse, TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
        }
    }

    private string FormatCountdown(int secondsLeft)
        => Config.CountdownText.Replace("{SECONDS}", secondsLeft.ToString());

    // -------------------------
    // Center output (simple + optional text color)
    // -------------------------
    private void BroadcastCenter(string text)
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (p == null || !p.IsValid || p.IsHLTV)
                continue;

            PrintCenter(p, text);
        }
    }

    private void PrintCenter(CCSPlayerController player, string plainText)
    {
        // If possible, use HTML to color the text only (no box/background styling)
        if (Config.UseColoredCenterText && _printToCenterHtmlExt != null)
        {
            string color = NormalizeHex(Config.TextColorHex, "#FF0000");
            string safe = EscapeHtml(plainText);
            string html = $"<font color=\"{color}\">{safe}</font>";
            _printToCenterHtmlExt.Invoke(null, new object[] { player, html });
            return;
        }

        // Fallback: plain
        player.PrintToCenter(plainText);
    }

    private static MethodInfo? FindPrintToCenterHtmlExtension()
    {
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (!t.IsSealed || !t.IsAbstract) continue; // static class
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        if (!string.Equals(m.Name, "PrintToCenterHtml", StringComparison.Ordinal)) continue;

                        var ps = m.GetParameters();
                        if (ps.Length != 2) continue;
                        if (ps[0].ParameterType != typeof(CCSPlayerController)) continue;
                        if (ps[1].ParameterType != typeof(string)) continue;

                        return m;
                    }
                }
            }
        }
        catch { }
        return null;
    }

    private static string NormalizeHex(string input, string fallback)
    {
        var s = input.Trim();
        if (!s.StartsWith("#", StringComparison.Ordinal)) s = "#" + s;
        if (s.Length != 7) return fallback;
        return s;
    }

    private static string EscapeHtml(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // -------------------------
    // True hide (transmit)
    // -------------------------
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (!_active)
            return;

        foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
        {
            if (viewer == null || !viewer.IsValid || viewer.IsHLTV)
                continue;

            foreach (var target in Utilities.GetPlayers())
            {
                if (target == null || !target.IsValid || target.IsHLTV)
                    continue;

                if (target == viewer)
                    continue;

                var pawn = target.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid)
                    continue;

                info.TransmitEntities.Remove(pawn);
            }
        }
    }

    // -------------------------
    // Damage block
    // -------------------------
    private HookResult OnEntityTakeDamagePre(CEntityInstance victim, CTakeDamageInfo info)
    {
        if (!_active)
            return HookResult.Continue;

        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (!string.Equals(victim.DesignerName, "player", StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        info.Damage = 0;
        return HookResult.Handled;
    }

    // -------------------------
    // Weapon resync pulse
    // -------------------------
    private void WeaponResyncPulse()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || player.IsHLTV)
                continue;

            player.ExecuteClientCommand("slot3; slot2; slot1");
        }
    }
}
