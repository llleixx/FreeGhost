using System;
using System.Reflection;
using BepInEx.Logging;
using FreeGhost.Core;
using HarmonyLib;
using Unity.Mathematics;
using UnityEngine;
using CoreVector2 = System.Numerics.Vector2;
using CoreVector3 = System.Numerics.Vector3;

namespace FreeGhost;

internal sealed class FreeGhostController
{
    private readonly ModConfig _config;
    private readonly ManualLogSource _logger;

    private PatchCapabilities _capabilities;
    private Func<Vector3, Vector3>? _directionToLook;
    private bool _sessionActive;
    private bool _freeModeActive;
    private bool _sprintToggleActive;
    private bool _crouchToggleActive;
    private bool _syncFailureLogged;
    private bool _hasLastEncodedLook;
    private CoreVector2 _lastEncodedLook;
    private bool _hasLastValidOriginal;
    private CharacterSyncData _lastValidOriginal;
    private Vector3 _freeModeOrigin;

    public FreeGhostController(ModConfig config, ManualLogSource logger)
    {
        _config = config;
        _logger = logger;
    }

    public Vector3 FreePosition { get; private set; }

    public void SetCapabilities(PatchCapabilities capabilities)
    {
        _capabilities = capabilities;
    }

    public bool TryBindDirectionConverter()
    {
        MethodInfo? method = AccessTools.Method(typeof(HelperFunctions), "DirectionToLook", new[] { typeof(Vector3) });
        if (method == null || method.ReturnType != typeof(Vector3))
        {
            _logger.LogError("HelperFunctions.DirectionToLook(Vector3) was not found for the PEAK 2.0.a baseline.");
            return false;
        }

        try
        {
            _directionToLook = (Func<Vector3, Vector3>)Delegate.CreateDelegate(typeof(Func<Vector3, Vector3>), method);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError($"Could not bind PEAK's direction-to-look converter: {exception}");
            return false;
        }
    }

    public void TickLifecycle()
    {
        if (!_sessionActive)
            return;

        Character? local = Character.localCharacter;
        if (!_config.Enabled.Value || !_capabilities.LocalMode || local == null || local.data == null ||
            !local.data.dead || !local.data.fullyPassedOut || local.Ghost == null)
        {
            Reset("local ghost lifecycle ended");
        }
    }

    public void AfterVanillaSpectate(MainCameraMovement cameraMovement)
    {
        Character? local = Character.localCharacter;
        if (!CanStartGhostSession(local))
        {
            if (_sessionActive)
                Reset("vanilla spectating ended");
            return;
        }

        if (!_sessionActive)
        {
            if (!EnterFreeMode(cameraMovement.transform, "ghost session started"))
                return;
            _sessionActive = true;
        }

        GUIManager? gui = GUIManager.instance;
        bool inputAllowed = Time.timeScale > 0f && gui != null && !gui.windowBlockingInput && !gui.wheelActive;
        if (inputAllowed && _config.ModeToggleShortcut.Value.IsDown())
        {
            if (_freeModeActive)
            {
                ExitFreeMode(local!, cameraMovement.transform);
                return;
            }

            if (!EnterFreeMode(cameraMovement.transform, "mode toggle"))
                return;
        }

        if (!_freeModeActive)
            return;

        if (inputAllowed)
        {
            HandleToggleInputs(local.input);
            ApplyMovement(cameraMovement.transform, local.input);
        }

        cameraMovement.transform.position = FreePosition;
        ApplyGhostTransform(local.Ghost, cameraMovement.transform.rotation);
    }

    public bool TryApplyLocalGhost(PlayerGhost ghost)
    {
        if (!_freeModeActive || !_capabilities.LocalMode)
            return false;

        Character? local = Character.localCharacter;
        if (local == null || local.Ghost == null || local.Ghost != ghost)
            return false;

        ApplyGhostTransform(ghost, ghost.transform.rotation);
        return true;
    }

    public void TryRewriteSyncData(CharacterSyncer syncer, ref CharacterSyncData data)
    {
        if (!_freeModeActive || !_capabilities.VanillaSync || !_config.SyncToVanillaClients.Value)
            return;

        Character? local = Character.localCharacter;
        Character? owner = syncer.GetComponent<Character>();
        if (local == null || owner == null || owner != local || !local.data.dead || local.Ghost == null)
            return;

        CharacterSyncData original = data;
        if (IsValidOriginalSync(original))
        {
            _lastValidOriginal = original;
            _hasLastValidOriginal = true;
        }

        // Encode against the target PlayerGhost.Update will actually use. During a spectate
        // transition, specCharacter can change one frame before RPCA_SetTarget updates m_target.
        Character? target = local.Ghost.m_target;
        if (target == null || !IsFinite(FreePosition) || _directionToLook == null)
        {
            RestoreSafeOriginal(ref data, original);
            return;
        }

        try
        {
            CoreVector3 center = ToCore(target.Center);
            CoreVector3 desired = ToCore(FreePosition);
            CoreVector2? previous = _hasLastEncodedLook ? _lastEncodedLook : null;

            if (!GhostPositionCodec.TrySolveDirection(center, desired, out CoreVector3 direction))
            {
                SyncFailedOnce("The desired ghost position could not be encoded safely.");
                RestoreSafeOriginal(ref data, original);
                return;
            }

            Vector3 gameLook = _directionToLook(ToUnity(direction));
            CoreVector2 finalLook = new(gameLook.x, gameLook.y);

            if (!GhostPositionCodec.TryEncodeAroundLook(center, desired, finalLook, previous, out GhostEncoding encoded))
            {
                SyncFailedOnce("PEAK's direction-to-look conversion produced an unsafe result.");
                RestoreSafeOriginal(ref data, original);
                return;
            }

            data.lookValues = new float2(encoded.LookValues.X, encoded.LookValues.Y);
            data.spectateZoom = encoded.Zoom;
            _lastEncodedLook = encoded.LookValues;
            _hasLastEncodedLook = true;
            _syncFailureLogged = false;
        }
        catch (Exception exception)
        {
            SyncFailedOnce($"Ghost position encoding failed: {exception.Message}");
            RestoreSafeOriginal(ref data, original);
        }
    }

    public void Reset(string reason)
    {
        if (_sessionActive)
            _logger.LogDebug($"Free ghost mode reset: {reason}.");

        _sessionActive = false;
        _freeModeActive = false;
        _sprintToggleActive = false;
        _crouchToggleActive = false;
        _hasLastEncodedLook = false;
        _hasLastValidOriginal = false;
        _syncFailureLogged = false;
        FreePosition = Vector3.zero;
        _freeModeOrigin = Vector3.zero;
    }

    private bool CanStartGhostSession(Character? local)
    {
        return _capabilities.LocalMode && _config.Enabled.Value && local != null && local.data != null &&
               local.data.dead && local.data.fullyPassedOut && local.Ghost != null;
    }

    private void HandleToggleInputs(CharacterInput input)
    {
        if (input.sprintToggleWasPressed)
            _sprintToggleActive = !_sprintToggleActive;
        if (input.crouchToggleWasPressed)
            _crouchToggleActive = !_crouchToggleActive;
    }

    private void ApplyMovement(Transform cameraTransform, CharacterInput input)
    {
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-8f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector2 planarInput = Vector2.ClampMagnitude(input.movementInput, 1f);
        float verticalInput = (input.jumpIsPressed ? 1f : 0f) -
                              (input.crouchIsPressed || _crouchToggleActive ? 1f : 0f);

        Vector3 movement = forward * planarInput.y + right * planarInput.x + Vector3.up * verticalInput;
        movement = Vector3.ClampMagnitude(movement, 1f);
        Vector3 next = FreePosition;
        if (movement.sqrMagnitude <= 1e-8f)
        {
            _sprintToggleActive = false;
        }
        else
        {
            bool sprinting = input.sprintIsPressed || _sprintToggleActive;
            float speed = _config.SafeBaseSpeed * (sprinting ? _config.SafeSprintMultiplier : 1f);
            next += movement * speed * Time.deltaTime;
        }

        if (IsFinite(next) && FreeFlightMath.TryClampToRadius(
                ToCore(_freeModeOrigin),
                ToCore(next),
                _config.SafeMaxDistance,
                out CoreVector3 clamped))
        {
            FreePosition = ToUnity(clamped);
        }
    }

    private bool EnterFreeMode(Transform cameraTransform, string reason)
    {
        Vector3 startPosition = cameraTransform.position;
        if (!IsFinite(startPosition))
            return false;

        FreePosition = startPosition;
        _freeModeOrigin = startPosition;
        _freeModeActive = true;
        _sprintToggleActive = false;
        _crouchToggleActive = false;
        _hasLastEncodedLook = false;
        _syncFailureLogged = false;
        _logger.LogDebug($"Free ghost mode activated: {reason}.");
        return true;
    }

    private void ExitFreeMode(Character local, Transform cameraTransform)
    {
        _freeModeActive = false;
        _sprintToggleActive = false;
        _crouchToggleActive = false;
        _hasLastEncodedLook = false;
        _syncFailureLogged = false;

        // Spectate() has already restored the vanilla camera this frame. Keep the local Ghost with it
        // so skipping PlayerGhost.Update earlier in the frame cannot leave a one-frame visual offset.
        if (local.Ghost != null)
            local.Ghost.transform.position = cameraTransform.position;

        _logger.LogDebug("Free ghost mode disabled; vanilla spectating resumed.");
    }

    private void ApplyGhostTransform(PlayerGhost ghost, Quaternion rotation)
    {
        if (!IsFinite(FreePosition))
            return;
        ghost.transform.SetPositionAndRotation(FreePosition, rotation);
    }

    private void RestoreSafeOriginal(ref CharacterSyncData data, CharacterSyncData original)
    {
        if (IsValidOriginalSync(original))
            return;

        if (_hasLastValidOriginal)
        {
            data.lookValues = _lastValidOriginal.lookValues;
            data.spectateZoom = _lastValidOriginal.spectateZoom;
            return;
        }

        data.lookValues = new float2(0f, 0f);
        data.spectateZoom = 2f;
    }

    private void SyncFailedOnce(string message)
    {
        if (_syncFailureLogged)
            return;
        _syncFailureLogged = true;
        _logger.LogWarning(message + " Falling back to valid vanilla sync values.");
    }

    private static bool IsValidOriginalSync(CharacterSyncData data)
    {
        return IsFinite(data.lookValues.x) && IsFinite(data.lookValues.y) && IsFinite(data.spectateZoom) &&
               Math.Abs(data.lookValues.x) <= HalfPrecision.MaxFinite &&
               Math.Abs(data.lookValues.y) <= HalfPrecision.MaxFinite &&
               Math.Abs(data.spectateZoom) <= HalfPrecision.MaxFinite;
    }

    private static CoreVector3 ToCore(Vector3 value) => new(value.x, value.y, value.z);
    private static Vector3 ToUnity(CoreVector3 value) => new(value.X, value.Y, value.Z);

    private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
