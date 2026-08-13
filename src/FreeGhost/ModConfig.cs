using BepInEx.Configuration;
using UnityEngine;

namespace FreeGhost;

internal sealed class ModConfig
{
    private const float DefaultBaseSpeed = 8f;
    private const float DefaultSprintMultiplier = 2.5f;
    private const float DefaultMaxDistance = 1000f;

    public ModConfig(ConfigFile config)
    {
        Enabled = config.Bind(
            "General",
            "Enabled",
            true,
            "Enable first-person free ghost mode after the local scout dies.");

        BaseSpeed = config.Bind(
            "Movement",
            "BaseSpeed",
            DefaultBaseSpeed,
            new ConfigDescription(
                "Base free-flight speed in meters per second.",
                new AcceptableValueRange<float>(0.1f, 1000f)));

        SprintMultiplier = config.Bind(
            "Movement",
            "SprintMultiplier",
            DefaultSprintMultiplier,
            new ConfigDescription(
                "Multiplier applied while the game's sprint input is active.",
                new AcceptableValueRange<float>(1f, 20f)));

        MaxDistance = config.Bind(
            "Movement",
            "MaxDistance",
            DefaultMaxDistance,
            new ConfigDescription(
                "Maximum free-flight distance in meters from the position where free mode was entered.",
                new AcceptableValueRange<float>(10f, 10000f)));

        ModeToggleShortcut = config.Bind(
            "Movement",
            "ModeToggleShortcut",
            new KeyboardShortcut(KeyCode.R),
            "Toggle between free ghost movement and PEAK's vanilla spectate camera.");

        SyncToVanillaClients = config.Bind(
            "Networking",
            "SyncToVanillaClients",
            true,
            "Encode the free ghost position into vanilla lookValues and spectateZoom fields.");
    }

    public ConfigEntry<bool> Enabled { get; }
    public ConfigEntry<float> BaseSpeed { get; }
    public ConfigEntry<float> SprintMultiplier { get; }
    public ConfigEntry<float> MaxDistance { get; }
    public ConfigEntry<KeyboardShortcut> ModeToggleShortcut { get; }
    public ConfigEntry<bool> SyncToVanillaClients { get; }

    public float SafeBaseSpeed => SafeRange(BaseSpeed.Value, 0.1f, 1000f, DefaultBaseSpeed);
    public float SafeSprintMultiplier => SafeRange(SprintMultiplier.Value, 1f, 20f, DefaultSprintMultiplier);
    public float SafeMaxDistance => SafeRange(MaxDistance.Value, 10f, 10000f, DefaultMaxDistance);

    private static float SafeRange(float value, float min, float max, float fallback)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return fallback;
        return Mathf.Clamp(value, min, max);
    }
}
