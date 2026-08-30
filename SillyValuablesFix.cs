using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace SillyValuablesFix
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("Sangrento.SillyValuables", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.neko3004.sillyvaluablesfix";
        public const string PluginName = "SillyValuables Fix";
        public const string PluginVersion = "1.2.1";

        internal static ManualLogSource Log = null!;
        private Harmony? harmony;

        private void Awake()
        {
            Log = Logger;

            try
            {
                harmony = new Harmony(PluginGuid);

                // Desativa os patches com métodos obsoletos do SillyValuables original
                UnpatchBrokenSillyValuablesPatches();

                // Aplica os patches de forma individual e segura
                SafePatch(typeof(Patch_PhotonView_Awake));
                SafePatch(typeof(Patch_PhotonView_SerializeComponent));
                SafePatch(typeof(Patch_PhotonView_DeserializeComponent));
                SafePatch(typeof(Patch_ItemMelee_Start));
                SafePatch(typeof(Patch_GranadaJogavel_Start));
                SafePatch(typeof(Patch_GranadaJogavelSemExplosao_Start));

                Log.LogInfo($"{PluginName} {PluginVersion} carregado e patches aplicados com sucesso.");
            }
            catch (Exception ex)
            {
                Log.LogError($"Erro geral no Awake: {ex}");
            }
        }

        private void SafePatch(Type patchType)
        {
            try
            {
                harmony?.CreateClassProcessor(patchType).Patch();
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Falha ao registrar patch '{patchType.Name}': {ex.Message}");
            }
        }

        private void UnpatchBrokenSillyValuablesPatches()
        {
            try
            {
                int unpatchedCount = 0;
                foreach (MethodBase method in Harmony.GetAllPatchedMethods().ToList())
                {
                    Patches patches = Harmony.GetPatchInfo(method);
                    if (patches == null) continue;

                    var allPatches = patches.Prefixes
                        .Concat(patches.Postfixes)
                        .Concat(patches.Transpilers)
                        .Concat(patches.Finalizers);

                    foreach (Patch patch in allPatches)
                    {
                        if (patch.owner != null && (patch.owner.IndexOf("SillyValuables", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            patch.PatchMethod.DeclaringType?.Assembly.GetName().Name?.IndexOf("SillyValuables", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            if (patch.owner.Equals(PluginGuid, StringComparison.OrdinalIgnoreCase))
                                continue;

                            harmony?.Unpatch(method, patch.PatchMethod);
                            unpatchedCount++;
                        }
                    }
                }

                if (unpatchedCount > 0)
                {
                    Log.LogInfo($"Desativados {unpatchedCount} patch(es) antigos/quebrados do SillyValuables original.");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Nao foi possivel desativar patches antigos: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        public static bool TryGetMember<T>(object? instance, string memberName, out T value)
        {
            value = default!;

            if (instance == null)
                return false;

            Type? type = instance.GetType();

            while (type != null)
            {
                const BindingFlags flags = BindingFlags.Instance |
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.DeclaredOnly;

                FieldInfo? field = type.GetField(memberName, flags);
                if (field != null)
                {
                    try
                    {
                        object? raw = field.GetValue(instance);
                        if (raw is T typed)
                        {
                            value = typed;
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogDebug($"Failed to read field '{memberName}': {ex.Message}");
                    }

                    return false;
                }

                PropertyInfo? property = type.GetProperty(memberName, flags);
                if (property != null && property.GetMethod != null)
                {
                    try
                    {
                        object? raw = property.GetValue(instance);
                        if (raw is T typed)
                        {
                            value = typed;
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogDebug($"Failed to read property '{memberName}': {ex.Message}");
                    }

                    return false;
                }

                type = type.BaseType;
            }

            return false;
        }

        public static bool TrySetMember(object? instance, string memberName, object? value)
        {
            if (instance == null)
                return false;

            Type? type = instance.GetType();

            while (type != null)
            {
                const BindingFlags flags = BindingFlags.Instance |
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.DeclaredOnly;

                FieldInfo? field = type.GetField(memberName, flags);
                if (field != null)
                {
                    try
                    {
                        field.SetValue(instance, value);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.LogDebug($"Failed to write field '{memberName}': {ex.Message}");
                        return false;
                    }
                }

                PropertyInfo? property = type.GetProperty(memberName, flags);
                if (property != null && property.SetMethod != null)
                {
                    try
                    {
                        property.SetValue(instance, value);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.LogDebug($"Failed to write property '{memberName}': {ex.Message}");
                        return false;
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        public static bool CheckIsEquipped(Component? item)
        {
            return TryGetMember(item, "isEquipped", out bool equipped) && equipped;
        }
    }

    // ==========================================
    // CORREÇÕES DO PHOTON VIEW (IPunObservable)
    // ==========================================

    [HarmonyPatch(typeof(PhotonView), "Awake")]
    internal static class Patch_PhotonView_Awake
    {
        private static readonly HashSet<string> loggedCleanups = new(StringComparer.OrdinalIgnoreCase);

        private static void Prefix(PhotonView __instance)
        {
            CleanObserved(__instance);
        }

        private static void Postfix(PhotonView __instance)
        {
            CleanObserved(__instance);
        }

        public static void CleanObserved(PhotonView view)
        {
            if (view == null || view.ObservedComponents == null || view.ObservedComponents.Count == 0)
                return;

            var invalidComponents = view.ObservedComponents
                .Where(comp => comp == null || !(comp is IPunObservable))
                .ToList();

            if (invalidComponents.Count > 0)
            {
                string rawName = view.gameObject != null ? view.gameObject.name : "Unknown Object";
                string cleanName = rawName.Replace("(Clone)", "").Trim();

                if (loggedCleanups.Add(cleanName))
                {
                    string componentTypes = string.Join(", ", invalidComponents.Select(c => c == null ? "null" : c.GetType().Name));
                    Plugin.Log.LogInfo($"[PhotonView Fix] Componentes incompatíveis removidos de '{cleanName}': [{componentTypes}]");
                }

                view.ObservedComponents.RemoveAll(comp => comp == null || !(comp is IPunObservable));
            }
        }
    }

    [HarmonyPatch(typeof(PhotonView), "SerializeComponent")]
    internal static class Patch_PhotonView_SerializeComponent
    {
        private static bool Prefix(Component component)
        {
            if (component == null || !(component is IPunObservable))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PhotonView), "DeserializeComponent")]
    internal static class Patch_PhotonView_DeserializeComponent
    {
        private static bool Prefix(Component component)
        {
            if (component == null || !(component is IPunObservable))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ItemMelee), "Start")]
    internal static class Patch_ItemMelee_Start
    {
        private static void Postfix(ItemMelee __instance)
        {
            if (__instance == null) return;

            PhotonView pv = __instance.GetComponent<PhotonView>();
            if (pv != null)
            {
                Patch_PhotonView_Awake.CleanObserved(pv);
            }
        }
    }

    // ==========================================
    // CORREÇÕES DAS GRANADAS DO SILLYVALUABLES
    // ==========================================

    [HarmonyPatch(typeof(GranadaJogavel), "Start")]
    internal static class Patch_GranadaJogavel_Start
    {
        private static void Postfix(GranadaJogavel __instance)
        {
            AttachFixer(__instance);
        }

        private static void AttachFixer(MonoBehaviour instance)
        {
            if (instance == null || instance.GetComponent<GranadaFixer>() != null)
                return;

            GranadaFixer fixer = instance.gameObject.AddComponent<GranadaFixer>();
            fixer.Setup(instance);
        }
    }

    [HarmonyPatch(typeof(GranadaJogavelSemExplosao), "Start")]
    internal static class Patch_GranadaJogavelSemExplosao_Start
    {
        private static void Postfix(GranadaJogavelSemExplosao __instance)
        {
            if (__instance == null || __instance.GetComponent<GranadaFixer>() != null)
                return;

            GranadaFixer fixer = __instance.gameObject.AddComponent<GranadaFixer>();
            fixer.Setup(__instance);
        }
    }

    internal sealed class GranadaFixer : MonoBehaviour
    {
        private MonoBehaviour? grenade;
        private AudioSource? replacementAudioSource;
        private GameObject? replacementAudioObject;

        private static AudioClip? cachedMineClip;
        private static bool searchedForClip;

        private float tickAudioTimer;
        private bool lastActive;
        private bool initialized;
        private bool shuttingDown;

        private const float MinePitch = 1.2f;
        private const float TickInterval = 0.35f;
        private const float AudioVolume = 0.95f;
        private const float AudioMinDistance = 4f;
        private const float AudioMaxDistance = 45f;

        public void Setup(MonoBehaviour original)
        {
            if (initialized || original == null)
                return;

            grenade = original;
            initialized = true;

            grenade.enabled = false;

            Plugin.TrySetMember(grenade, "soundSplinter", null);

            FindGameMineSound();
            DisableOriginalAudioSources();
            CreateReplacementAudioSource();

            Plugin.Log.LogInfo($"Original function broken, applying fix to: {gameObject.name}");
        }

        private static void FindGameMineSound()
        {
            if (searchedForClip)
                return;

            searchedForClip = true;

            AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();

            cachedMineClip = clips.FirstOrDefault(c => NameEquals(c, "item explosive mine warning beeps"))
                ?? clips.FirstOrDefault(c => NameEquals(c, "grenade countdown"))
                ?? clips.FirstOrDefault(c => NameEquals(c, "item explosive mine arm beep"))
                ?? clips.FirstOrDefault(c => NameEquals(c, "snd_dirt_tracker_beep"));

            if (cachedMineClip == null)
                Plugin.Log.LogWarning("Could not find the grenade/mine warning beep AudioClip.");
            else
                Plugin.Log.LogInfo($"Using warning beep AudioClip: {cachedMineClip.name}");
        }

        private static bool NameEquals(AudioClip clip, string name)
        {
            return clip != null &&
                   clip.name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        private void DisableOriginalAudioSources()
        {
            AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);

            foreach (AudioSource source in sources)
            {
                source.Stop();
                source.playOnAwake = false;
                source.enabled = false;
            }
        }

        private void CreateReplacementAudioSource()
        {
            if (replacementAudioSource != null || cachedMineClip == null)
                return;

            replacementAudioObject = new GameObject("SillyValuablesFix_Audio");
            replacementAudioObject.transform.SetParent(transform, false);
            replacementAudioObject.transform.localPosition = Vector3.zero;

            replacementAudioSource = replacementAudioObject.AddComponent<AudioSource>();
            replacementAudioSource.enabled = true;
            replacementAudioSource.playOnAwake = false;
            replacementAudioSource.loop = false;
            replacementAudioSource.clip = cachedMineClip;
            replacementAudioSource.volume = AudioVolume;
            replacementAudioSource.pitch = MinePitch;
            replacementAudioSource.spatialBlend = 1f;
            replacementAudioSource.rolloffMode = AudioRolloffMode.Linear;
            replacementAudioSource.minDistance = AudioMinDistance;
            replacementAudioSource.maxDistance = AudioMaxDistance;
            replacementAudioSource.dopplerLevel = 0f;
            replacementAudioSource.ignoreListenerPause = false;
        }

        private void PlayCustomMineTick()
        {
            if (shuttingDown ||
                replacementAudioSource == null ||
                replacementAudioObject == null ||
                cachedMineClip == null)
            {
                return;
            }

            if (!replacementAudioObject.activeInHierarchy)
                return;

            if (!replacementAudioSource.enabled)
                replacementAudioSource.enabled = true;

            replacementAudioSource.transform.position = transform.position;
            replacementAudioSource.pitch = MinePitch;
            replacementAudioSource.PlayOneShot(cachedMineClip, 1f);
        }

        private bool TryInvoke(string methodName)
        {
            MonoBehaviour? currentGrenade = grenade;
            if (currentGrenade == null)
                return false;

            try
            {
                MethodInfo? method = FindMethod(currentGrenade.GetType(), methodName);
                if (method == null)
                {
                    Plugin.Log.LogWarning($"Method '{methodName}' was not found on {currentGrenade.GetType().Name}.");
                    return false;
                }

                method.Invoke(currentGrenade, null);
                return true;
            }
            catch (TargetInvocationException ex)
            {
                Plugin.Log.LogError($"Exception inside '{methodName}': {ex.InnerException ?? ex}");
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to invoke '{methodName}': {ex}");
                return false;
            }
        }

        private static MethodInfo? FindMethod(Type? type, string methodName)
        {
            const BindingFlags flags = BindingFlags.Instance |
                                        BindingFlags.Public |
                                        BindingFlags.NonPublic |
                                        BindingFlags.DeclaredOnly;

            while (type != null)
            {
                MethodInfo? method = type.GetMethod(methodName, flags, null, Type.EmptyTypes, null);
                if (method != null)
                    return method;

                type = type.BaseType;
            }

            return null;
        }

        private bool TryGetState(out bool active, out bool thrownRocket, out bool equipped, out Component? physGrab)
        {
            active = false;
            thrownRocket = false;
            equipped = false;
            physGrab = null;

            MonoBehaviour? currentGrenade = grenade;
            if (currentGrenade == null)
                return false;

            if (!Plugin.TryGetMember(currentGrenade, "isActive", out active))
                return false;

            Plugin.TryGetMember(currentGrenade, "isThrownRocket", out thrownRocket);
            Plugin.TryGetMember(currentGrenade, "itemEquippable", out Component? equippable);
            Plugin.TryGetMember(currentGrenade, "physGrabObject", out physGrab);
            equipped = Plugin.CheckIsEquipped(equippable);
            return true;
        }

        private static bool IsGrabbed(Component? physGrabObject)
        {
            return Plugin.TryGetMember(physGrabObject, "grabbed", out bool grabbed) && grabbed;
        }

        private void ResetArmedState()
        {
            MonoBehaviour? currentGrenade = grenade;
            if (currentGrenade == null)
                return;

            Plugin.TrySetMember(currentGrenade, "isActive", false);
            Plugin.TrySetMember(currentGrenade, "grenadeTimer", 0f);
            Plugin.TrySetMember(currentGrenade, "splinterAnimationProgress", 0f);
            Plugin.TrySetMember(currentGrenade, "isThrownRocket", false);

            if (Plugin.TryGetMember(currentGrenade, "itemToggle", out Component? itemToggle) && itemToggle != null)
                TryInvokeOnComponent(itemToggle, "ToggleItem", false, -1);

            if (Plugin.TryGetMember(currentGrenade, "splinterTransform", out Transform? splinterTransform) && splinterTransform != null)
                splinterTransform.localEulerAngles = Vector3.zero;

            if (Plugin.TryGetMember(currentGrenade, "grenadeEmissionMaterial", out Material? material) && material != null)
                material.SetColor("_EmissionColor", Color.black);

            if (Plugin.TryGetMember(currentGrenade, "throwLine", out GameObject? throwLine) && throwLine != null)
                throwLine.SetActive(false);
        }

        private static bool TryInvokeOnComponent(Component component, string methodName, params object[] args)
        {
            try
            {
                MethodInfo? method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                    return false;

                method.Invoke(component, args);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"Failed to invoke component method '{methodName}': {ex.Message}");
                return false;
            }
        }

        private void Update()
        {
            if (shuttingDown || !initialized || grenade == null)
                return;

            if (!TryGetState(out bool active, out bool thrownRocket, out bool equipped, out Component? physGrab))
                return;

            bool grabbed = IsGrabbed(physGrab);

            if (equipped)
            {
                if (active)
                    ResetArmedState();

                lastActive = false;
                tickAudioTimer = 0f;
                return;
            }

            if (!active && lastActive)
                tickAudioTimer = 0f;

            if (active)
            {
                tickAudioTimer += Time.deltaTime;

                while (tickAudioTimer >= TickInterval)
                {
                    tickAudioTimer -= TickInterval;
                    PlayCustomMineTick();
                }

                UpdatePinAnimation();
                UpdateEmission();
            }

            if (physGrab != null && !equipped && active && !thrownRocket && !grabbed)
            {
                Plugin.TrySetMember(grenade, "isThrownRocket", true);
                TryInvoke("LaunchAsRocket");
            }

            TryStartActivation();
            lastActive = active;
        }

        private void TryStartActivation()
        {
            MonoBehaviour? currentGrenade = grenade;
            if (currentGrenade == null || !SemiFunc.IsMasterClientOrSingleplayer())
                return;

            if (!Plugin.TryGetMember(currentGrenade, "itemToggle", out Component? toggle) || toggle == null)
                return;

            if (!Plugin.TryGetMember(toggle, "toggleState", out bool toggleState) || !toggleState)
                return;

            if (Plugin.TryGetMember(currentGrenade, "isActive", out bool active) && active)
                return;

            Plugin.TrySetMember(currentGrenade, "isActive", true);
            tickAudioTimer = 0f;
            PlayCustomMineTick();
            TryInvoke("TickStart");
        }

        private void UpdatePinAnimation()
        {
            MonoBehaviour? currentGrenade = grenade;
            if (currentGrenade == null)
                return;

            if (!Plugin.TryGetMember(currentGrenade, "splinterAnimationProgress", out float progress) ||
                !Plugin.TryGetMember(currentGrenade, "splinterAnimationCurve", out AnimationCurve? curve) ||
                !Plugin.TryGetMember(currentGrenade, "splinterTransform", out Transform? splinterTransform))
                return;

            if (progress < 1f)
            {
                progress = Mathf.Min(1f, progress + 5f * Time.deltaTime);
                Plugin.TrySetMember(currentGrenade, "splinterAnimationProgress", progress);
            }

            if (splinterTransform != null)
            {
                float value = curve != null ? curve.Evaluate(progress) : 0f;
                splinterTransform.localEulerAngles = new Vector3(value * 90f, 0f, 0f);
            }
        }

        private void UpdateEmission()
        {
            MonoBehaviour? currentGrenade = grenade;
            if (currentGrenade == null)
                return;

            if (!Plugin.TryGetMember(currentGrenade, "blinkColor", out Color blinkColor))
                return;

            if (!Plugin.TryGetMember(currentGrenade, "grenadeEmissionMaterial", out Material? material) || material == null)
                return;

            float blink = Mathf.PingPong(Time.time * 8f, 1f);
            material.SetColor("_EmissionColor", blinkColor * Mathf.LinearToGammaSpace(blink));
        }

        private void FixedUpdate()
        {
            if (shuttingDown || !initialized || grenade == null)
                return;

            MonoBehaviour currentGrenade = grenade;

            if (!Plugin.TryGetMember(currentGrenade, "itemEquippable", out Component? equippable) ||
                !Plugin.TryGetMember(currentGrenade, "rb", out Rigidbody? rb) ||
                !Plugin.TryGetMember(currentGrenade, "prevPosition", out Vector3 previousPosition))
                return;

            Plugin.TryGetMember(currentGrenade, "physGrabObject", out Component? physGrab);
            bool equipped = Plugin.CheckIsEquipped(equippable);
            bool grabbed = IsGrabbed(physGrab);
            Plugin.TryGetMember(equippable, "wasEquippedTimer", out float wasEquippedTimer);

            if (rb == null)
                return;

            if (equipped || wasEquippedTimer > 0f)
            {
                Plugin.TrySetMember(currentGrenade, "prevPosition", rb.position);
                return;
            }

            Vector3 velocity = (rb.position - previousPosition) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            Plugin.TrySetMember(currentGrenade, "prevPosition", rb.position);

            if (!grabbed && velocity.magnitude > 2f)
                Plugin.TrySetMember(currentGrenade, "throwLineTimer", 0.2f);

            Plugin.TryGetMember(currentGrenade, "throwLineTimer", out float throwLineTimer);

            if (Plugin.TryGetMember(currentGrenade, "throwLineTrail", out TrailRenderer? trail) && trail != null)
                trail.emitting = throwLineTimer > 0f;

            if (throwLineTimer > 0f)
                Plugin.TrySetMember(currentGrenade, "throwLineTimer", throwLineTimer - Time.fixedDeltaTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (shuttingDown || grenade == null)
                return;

            if (Plugin.TryGetMember(grenade, "isThrownRocket", out bool thrownRocket) &&
                thrownRocket &&
                SemiFunc.IsMasterClientOrSingleplayer())
            {
                TryInvoke("TickEnd");
            }
        }

        private void OnDestroy()
        {
            shuttingDown = true;

            if (replacementAudioSource != null)
            {
                replacementAudioSource.Stop();
                replacementAudioSource.enabled = false;
                replacementAudioSource = null;
            }

            if (replacementAudioObject != null)
            {
                Destroy(replacementAudioObject);
                replacementAudioObject = null;
            }
        }
    }
}