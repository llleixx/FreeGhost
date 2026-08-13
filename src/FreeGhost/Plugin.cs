using BepInEx;
using HarmonyLib;

namespace FreeGhost;

[BepInPlugin(PluginGuid, PluginName, BuildInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.github.lllei.FreeGhost";
    public const string PluginName = "FreeGhost";

    internal static Plugin? Instance { get; private set; }
    internal ModConfig Settings { get; private set; } = null!;
    internal FreeGhostController Controller { get; private set; } = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        Instance = this;
        Settings = new ModConfig(Config);
        Controller = new FreeGhostController(Settings, Logger);
        _harmony = new Harmony(PluginGuid);

        PatchCapabilities capabilities = PatchInstaller.Install(_harmony, Logger, Controller);
        Controller.SetCapabilities(capabilities);
        Logger.LogInfo($"{PluginName} {BuildInfo.Version} loaded for PEAK 2.0.a baseline. " +
                       $"Local mode: {capabilities.LocalMode}; vanilla sync: {capabilities.VanillaSync}.");
    }

    private void Update()
    {
        Controller?.TickLifecycle();
    }

    private void OnDestroy()
    {
        Controller?.Reset("plugin destroyed");
        _harmony?.UnpatchSelf();
        Instance = null;
    }
}
