using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace FreeGhost;

internal readonly struct PatchCapabilities
{
    public PatchCapabilities(bool localMode, bool vanillaSync)
    {
        LocalMode = localMode;
        VanillaSync = vanillaSync;
    }

    public bool LocalMode { get; }
    public bool VanillaSync { get; }
}

internal static class PatchInstaller
{
    public static PatchCapabilities Install(Harmony harmony, ManualLogSource logger, FreeGhostController controller)
    {
        bool cameraPatched = TryPatch(
            harmony,
            AccessTools.Method(typeof(MainCameraMovement), "Spectate"),
            postfix: AccessTools.Method(typeof(PatchCallbacks), nameof(PatchCallbacks.CameraSpectatePostfix)),
            description: "MainCameraMovement.Spectate",
            logger: logger);

        bool ghostPatched = TryPatch(
            harmony,
            AccessTools.Method(typeof(PlayerGhost), "Update"),
            prefix: AccessTools.Method(typeof(PatchCallbacks), nameof(PatchCallbacks.PlayerGhostUpdatePrefix)),
            description: "PlayerGhost.Update",
            logger: logger);

        bool localMode = cameraPatched && ghostPatched;
        if (!localMode)
            logger.LogError("Free ghost camera/model control is disabled because a required PEAK method was not found or patched.");

        bool directionConverter = controller.TryBindDirectionConverter();
        bool syncPatched = directionConverter && TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterSyncer), nameof(CharacterSyncer.GetDataToWrite)),
            postfix: AccessTools.Method(typeof(PatchCallbacks), nameof(PatchCallbacks.CharacterSyncPostfix)),
            description: "CharacterSyncer.GetDataToWrite",
            logger: logger);

        if (!syncPatched)
            logger.LogError("Vanilla-client ghost position sync is disabled; local free ghost mode can still operate.");

        return new PatchCapabilities(localMode, syncPatched);
    }

    private static bool TryPatch(
        Harmony harmony,
        MethodInfo? original,
        MethodInfo? prefix = null,
        MethodInfo? postfix = null,
        string? description = null,
        ManualLogSource? logger = null)
    {
        if (original == null || (prefix == null && postfix == null))
        {
            logger?.LogError($"Required patch target is missing: {description}.");
            return false;
        }

        try
        {
            harmony.Patch(
                original,
                prefix == null ? null : new HarmonyMethod(prefix),
                postfix == null ? null : new HarmonyMethod(postfix));
            return true;
        }
        catch (Exception exception)
        {
            logger?.LogError($"Failed to patch {description}: {exception}");
            return false;
        }
    }
}

internal static class PatchCallbacks
{
    public static void CameraSpectatePostfix(MainCameraMovement __instance)
    {
        Plugin.Instance?.Controller.AfterVanillaSpectate(__instance);
    }

    public static bool PlayerGhostUpdatePrefix(PlayerGhost __instance)
    {
        FreeGhostController? controller = Plugin.Instance?.Controller;
        return controller == null || !controller.TryApplyLocalGhost(__instance);
    }

    public static void CharacterSyncPostfix(CharacterSyncer __instance, ref CharacterSyncData __result)
    {
        Plugin.Instance?.Controller.TryRewriteSyncData(__instance, ref __result);
    }
}
