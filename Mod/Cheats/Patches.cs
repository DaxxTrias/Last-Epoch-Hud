using UnityEngine;
using MelonLoader;
using Il2Cpp;
// using Il2CppDMM;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppLE.UI;
using Il2CppLE.Telemetry;
using Il2CppItemFiltering;
using Il2CppLidgren.Network;
using Il2CppSystem.Net;
using Il2CppSteamworks;
using HarmonyLib;
using System.Linq;
using Mod.Cheats;

using HarmonyPatch = HarmonyLib.HarmonyPatch;
using static MelonLoader.LoaderConfig;
using static Il2Cpp.GroundItemManager;
using Mod.Utils;
using Mod.Game;


namespace Mod.Cheats.Patches
{
    public class MapIconPatch : MonoBehaviour
    {
        // private bool isInitialized = false;
        private static readonly string Icon_Base64 = SpriteBases.npcMapIcon;

        [HarmonyPatch(typeof(ActorSync), nameof(ActorSync.ReceiveInitDisplayInformation))]
        private static class ActorSync_MessageSyncRarit
        {
            private static void Postfix(ActorSync __instance, byte rarity)
            {
                try
                {

                }
                catch (Exception)
                {

                }
            }
        }

        [HarmonyPatch]
        internal class HarmonyPatches
        {
            //todo: verify that the patches are working
            //todo: verify there arent any new methods that should be patched
            //todo: verify that we patched out the unreal crash handler

            #region security / detection patches
            [HarmonyPatch(typeof(UIBase), "Awake")]
            public class UIBase_Awake : MelonMod
            {
                public static void Prefix(ref UIBase __instance)
                {
                    MelonLogger.Msg("[LeHud.Hooks]  UIBase.Awake hooked. Disabling bug submission button");
                    //__instance.gameObject.SetActive(false);
                    if (__instance != null)
                    {
                        var bugButton = __instance.bugReportButton;
                        if (bugButton != null && bugButton.gameObject != null)
                            bugButton.gameObject.SetActive(false);
                        var bugPanel = __instance.bugReportPanel;
                        if (bugPanel != null)
                            bugPanel.Close();

                        AutoDisconnect.SetUIBase(__instance);
                    }
                }
            }

            [HarmonyPatch(typeof(CharacterSelect), "Awake")]
            public class CharacterSelect_ : MelonMod
            {
                public static void Prefix(ref CharacterSelect __instance)
                {
                    MelonLogger.Msg("[LeHud.Hooks]  CharacterSelect.Awake hooked. Disabling bug submission button");
                    if (__instance != null && __instance.submitBugReportButton != null && __instance.submitBugReportButton.gameObject != null)
                        __instance.submitBugReportButton.gameObject.SetActive(false);
                }
            }

            [HarmonyPatch(typeof(UIBase), "OpenBugReportPanel")]
            public class UIBase_OpenBugReportPanel : MelonMod
            {
                public static bool Prefix(ref UIBase __instance)
                {

                    MelonLogger.Msg("[LeHud.Hooks]  UIBase.OpenBugReportPanel hooked and blocked.");
                    //__instance.gameObject.SetActive(false);
                    if (__instance != null)
                    {
                        var bugButton = __instance.bugReportButton;
                        if (bugButton != null && bugButton.gameObject != null)
                            bugButton.gameObject.SetActive(false);
                        var bugPanel = __instance.bugReportPanel;
                        if (bugPanel != null)
                            bugPanel.Close();
                    }
                    return false;
                }
            }

            [HarmonyPatch(typeof(BugSubmitter), "Submit")]
            public class BugSubmitter_Submit : MelonMod
            {
                public static bool Prefix(ref BugSubmitter __instance)
                {
                    MelonLogger.Msg("[LeHud.Hooks]  BugSubmitter.Submit hooked and blocked.");
                    if (__instance != null && __instance.gameObject != null)
                        __instance.gameObject.SetActive(false);
                    return false;
                }
            }

            [HarmonyPatch(typeof(BugSubmitter), "ShowSubmitPanel")]
            public class BugSubmitter_ShowSubmitPanel : MelonMod
            {
                public static bool Prefix(ref BugSubmitter __instance)
                {
                    MelonLogger.Msg("[LeHud.Hooks]  BugSubmitter.ShowSubmitPanel hooked and blocked.");
                    if (__instance != null && __instance.btn_Submit != null && __instance.btn_Submit.gameObject != null)
                        __instance.btn_Submit.gameObject.SetActive(false);
                    return false;
                }
            }

            [HarmonyPatch(typeof(ClientLogHandler), nameof(ClientLogHandler.LogFormat),
                typeof(LogType), typeof(UnityEngine.Object), typeof(string), typeof(Il2CppReferenceArray<Il2CppSystem.Object>))]
            public class ClientLogHandler_LogFormat : MelonMod
            {
                public static bool Prefix(LogType logType, UnityEngine.Object context, string format, Il2CppReferenceArray<Il2CppSystem.Object> args)
                {
                    //MelonLogger.Msg("[Mod] ClientLogHandler.LogFormat hooked and blocked.");

                    // Log all elements
                    //MelonLogger.Msg($"LogType: {logType}");
                    //MelonLogger.Msg($"Context: {context?.name ?? "null"}");
                    //MelonLogger.Msg($"Format: {format}");

                    //if (args != null)
                    //{
                    //	for (int i = 0; i < args.Length; i++)
                    //	{
                    //		MelonLogger.Msg($"Arg[{i}]: {args[i]?.ToString() ?? "null"}");
                    //	}
                    //}
                    //else
                    //{
                    //	MelonLogger.Msg("Args: null");
                    //}

                    return false;
                }
            }

            [HarmonyPatch(typeof(ClientLogHandler), nameof(ClientLogHandler.LogException),
                typeof(Il2CppSystem.Exception), typeof(UnityEngine.Object))]
            public class ClientLogHandler_LogException : MelonMod
            {
                public static bool Prefix(Il2CppSystem.Exception exception, UnityEngine.Object context)
                {
                    MelonLogger.Msg("[LeHud.Hooks]  ClientLogHandler.LogException hooked and blocked.");

                    // Log all elements
                    //MelonLogger.Msg($"Context: {context?.name ?? "null"}");
                    //MelonLogger.Msg($"Exception: {exception?.ToString() ?? "null"}");

                    return false;
                }
            }

            [HarmonyPatch(typeof(AccountSupport), "GetLogsZip")]
            public class AccountSupport_GetLogsZip : MelonMod
            {
                public static bool Prefix(AccountSupport __instance)
                {
                    MelonLogger.Msg("[LeHud.Hooks]  AccountSupport_GetLogsZip hooked and blocked.");
                    return false;
                }
            }

            //[HarmonyPatch(typeof(CharacterSelectPanelUI), "OpenBugReport")]
            //public class CharacterSelectPanelUI_OpenBugReport : MelonMod
            //{
            //	public static void Prefix(ref CharacterSelectPanelUI __instance)
            //	{
            //		MelonLogger.Msg("[Mod] CharacterSelectPanelUI.OpenBugReport hooked. Disabling bug submission");
            //		//__instance.gameObject.SetActive(false);
            //		return;
            //	}
            //}

            //[HarmonyPatch(typeof(LandingZonePanel), "OnAwake")]
            //public class LandingZonePanel_ : MelonMod
            //{
            //	public static void Prefix(ref LandingZonePanel __instance)
            //	{
            //		MelonLogger.Msg("[Mod] LandingZonePanel.OnAwake hooked");
            //	}
            //}
            #endregion

            #region active game patches
            [HarmonyPatch]
            [HarmonyPatch(typeof(CameraManager), "ApplyZoom")]
            public class Camera_ : MelonMod
            {
                //todo: getting kicked due to idle can cause this to stop triggering somehow
                //todo: breaks when switching between offline and online
                private static bool isPatched = false;
                public static void Postfix(CameraManager __instance)
                {
                    if (!isPatched && Settings.cameraZoomUnlock)
                    {
                        //MelonLogger.Msg("[Mod] CameraManager hooked");
                        //MelonLogger.Msg("zoomDefault: " + __instance.zoomDefault.ToString());
                        //MelonLogger.Msg("zoomMin: " + __instance.zoomMin.ToString());
                        //MelonLogger.Msg("reverseZoomDirection: " + __instance.reverseZoomDirection.ToString());
                        __instance.zoomDefault = -52.5f;
                        isPatched = true;
                        MelonLogger.Msg("[LeHud.Hooks]  Camera max zoom patched (3x)");
                        // zoomDefault: -17.5
                        // zoomMin: -7
                    }
                    else if (isPatched && !Settings.cameraZoomUnlock)
                    {
                        //MelonLogger.Msg("[Mod] CameraManager unhooked");
                        __instance.zoomDefault = -17.5f;
                        isPatched = false;
                        MelonLogger.Msg("[LeHud.Hooks]  Camera max zoom unpatched (1x)");
                    }
                }
            }

            [HarmonyPatch]
            [HarmonyPatch(typeof(DMMapZoom), "ZoomOutMinimap")]
            public class DMMapZoom_ZoomOutMinimap : MelonMod
            {
                private static bool isPatched = false;
                public static void Prefix(ref DMMapZoom __instance)
                {
                    if (!isPatched && Settings.minimapZoomUnlock)
                    {
                        //MelonLogger.Msg("DMMapZoom hooked");
                        //MelonLogger.Msg("minimap zoomDefault: " + __instance.maxMinimapZoom.ToString());
                        __instance.maxMinimapZoom = float.MaxValue;
                        isPatched = true;
                        MelonLogger.Msg("[LeHud.Hooks]  minimap max zoom patched ()");
                        // zoomdefault: 37.5
                    }
                    else if (isPatched && !Settings.minimapZoomUnlock)
                    {
                        //MelonLogger.Msg("[Mod] DMMapZoom unhooked");
                        __instance.maxMinimapZoom = 37.5f;
                        isPatched = false;
                        MelonLogger.Msg("[LeHud.Hooks]  minimap max zoom unpatched (1x)");
                    }
                }
            }
            #endregion

            #region waypoint patches
            /*
            [HarmonyPatch]
            public class WaypointManager_WaypointsEnabled_Any : MelonMod
            {
                private static System.Reflection.MethodBase? s_target;
                private static bool s_loggedToggle;

                [HarmonyPrepare]
                public static bool Prepare()
                {
                    try
                    {
                        var t = typeof(WaypointManager);
                        // Scan once without probing missing names to avoid noisy AccessTools warnings
                        var methods = AccessTools.GetDeclaredMethods(t) ?? new List<System.Reflection.MethodInfo>();

                        // Prefer exact method if present
                        s_target = methods.FirstOrDefault(m => m.IsStatic && m.ReturnType == typeof(bool) && m.GetParameters().Length == 0 && string.Equals(m.Name, "WaypointIsEnabled", StringComparison.Ordinal));

                        // Fallback: any static bool, parameterless method whose name contains both "waypoint" and "enable"
                        if (s_target == null)
                        {
                            s_target = methods.FirstOrDefault(m => m.IsStatic && m.ReturnType == typeof(bool) && m.GetParameters().Length == 0 && m.Name.IndexOf("waypoint", StringComparison.OrdinalIgnoreCase) >= 0 && m.Name.IndexOf("enable", StringComparison.OrdinalIgnoreCase) >= 0);
                        }

                        if (s_target == null)
                        {
                            var names = string.Join(", ", methods.Select(m => m.Name).Distinct());
                            MelonLogger.Warning($"[LeHud.Hooks]  WaypointManager.*WaypointsEnabled* not found; available methods: {names}");
                            return false;
                        }

                        MelonLogger.Msg($"[LeHud.Hooks]  WaypointManager global enable hook -> {s_target.Name}");
                        return true;
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  WaypointManager.*WaypointsEnabled* Prepare error: {e.Message}");
                        return false;
                    }
                }

                [HarmonyTargetMethod]
                public static System.Reflection.MethodBase TargetMethod() => s_target!;

                public static void Postfix(ref bool __result)
                {
                    try
                    {
                        if (Settings.useAnyWaypoint)
                        {
                            __result = true;
                            if (!s_loggedToggle)
                            {
                                s_loggedToggle = true;
                                MelonLogger.Msg("[LeHud.Hooks]  WaypointsEnabled() forced TRUE (useAnyWaypoint)");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  WaypointManager.*WaypointsEnabled* Postfix error: {e.Message}");
                    }
                }
            }

            // Instance-level property getter guard to keep UI/controller checks aligned
            [HarmonyPatch]
            public class WaypointManager_WaypointEnabled_PropertyGet : MelonMod
            {
                private static System.Reflection.MethodBase? s_target;

                [HarmonyPrepare]
                public static bool Prepare()
                {
                    try
                    {
                        var t = typeof(WaypointManager);
                        s_target = AccessTools.PropertyGetter(t, "WaypointEnabled")
                                || AccessTools.DeclaredMethod(t, "get_WaypointEnabled");
                        if (s_target == null)
                        {
                            MelonLogger.Warning("[LeHud.Hooks]  WaypointManager.get_WaypointEnabled not found; skipping patch.");
                            return false;
                        }
                        MelonLogger.Msg($"[LeHud.Hooks]  WaypointManager property getter hook -> {s_target.Name}");
                        return true;
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  WaypointManager.get_WaypointEnabled Prepare error: {e.Message}");
                        return false;
                    }
                }

                [HarmonyTargetMethod]
                public static System.Reflection.MethodBase TargetMethod() => s_target!;

                public static void Postfix(ref bool __result)
                {
                    try
                    {
                        if (Settings.useAnyWaypoint)
                            __result = true;
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  WaypointManager.get_WaypointEnabled Postfix error: {e.Message}");
                    }
                }
            }

            [HarmonyPatch(typeof(WaypointManager), "EnableWaypoint")]
            public class WaypointManager_EnableWaypoint : MelonMod
            {
                private static readonly System.Reflection.FieldInfo? waypointEnabledField = AccessTools.Field(typeof(WaypointManager), "waypointEnabled");
                private static readonly System.Reflection.FieldInfo? waypointsEnabledField = AccessTools.Field(typeof(WaypointManager), "waypointsEnabled");
                private static bool loggedMissingFields = false;
                private static bool loggedApplied = false;

                public static void Postfix(WaypointManager __instance)
                {
                    if (!Settings.useAnyWaypoint || __instance == null)
                        return;

                    try
                    {
                        bool anySet = false;
                        if (waypointEnabledField != null)
                        {
                            waypointEnabledField.SetValue(__instance, true);
                            anySet = true;
                        }
                        if (waypointsEnabledField != null)
                        {
                            waypointsEnabledField.SetValue(__instance, true);
                            anySet = true;
                        }

                        if (!anySet && !loggedMissingFields)
                        {
                            loggedMissingFields = true;
                            MelonLogger.Warning("[LeHud.Hooks]  WaypointManager.EnableWaypoint Postfix: enablement fields not found; relying on WaypointsEnabled()/WaypointIsEnabled() overrides only.");
                        }
                        else if (anySet && !loggedApplied)
                        {
                            loggedApplied = true;
                            MelonLogger.Msg("[LeHud.Hooks]  WaypointManager.EnableWaypoint forced fields TRUE");
                        }
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  WaypointManager.EnableWaypoint Postfix error: {e.Message}");
                    }
                }
            }

            [HarmonyPatch(typeof(WaypointCondition), "CheckWaypoint", new Type[] { typeof(CharacterDataTracker), typeof(string) })]
            public class WaypointCondition_CheckWaypoint
            {
                public static void Postfix(CharacterDataTracker dataTracker, string waypointSceneName, ref bool __result)
                {
                    try
                    {
                        if (Settings.useAnyWaypoint)
                            __result = true;
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  WaypointCondition.CheckWaypoint Postfix error: {e.Message}");
                    }
                }
            }

            [HarmonyPatch(typeof(UIWaypointStandard), "LoadWaypointScene")]
            public class UIWaypointStandard_LoadWaypointScene
            {
                public static void Prefix(UIWaypointStandard __instance)
                {
                    try
                    {
                        if (!Settings.useAnyWaypoint)
                            return;

                        // Make sure the UI element is considered active
                        __instance.isActive = true;

                        // Attempt to globally enable via manager instance if available
                        try
                        {
                            var mgr = WaypointManager.getInstance();
                            if (mgr != null)
                            {
                                mgr.EnableWaypoint();
                            }
                        }
                        catch { }
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  UIWaypointStandard.LoadWaypointScene Prefix error: {e.Message}");
                    }
                }
            }

            [HarmonyPatch(typeof(PlayerSync), "SendAttemptWaypoint", new Type[] { typeof(string), typeof(byte) })]
            public class PlayerSync_SendAttemptWaypoint
            {
                private static bool s_loggedOnce;
                public static void Prefix(PlayerSync __instance, string scene, byte gate)
                {
                    try
                    {
                        if (!s_loggedOnce && Settings.useAnyWaypoint)
                        {
                            s_loggedOnce = true;
                            // Minimal one-time trace to confirm path is hit
                            MelonLogger.Msg("[LeHud.Hooks]  PlayerSync.SendAttemptWaypoint observed");
                        }
                    }
                    catch { }
                }
            }
            */
            #endregion

            #region risky game patches
            [HarmonyPatch(typeof(UIWaypointStandard), "OnPointerEnter", new Type[] { typeof(UnityEngine.EventSystems.PointerEventData) })]
            internal class WayPointUnlock
            {
                public static void Prefix(UIWaypointStandard __instance, UnityEngine.EventSystems.PointerEventData eventData)
                {
                    //MelonLogger.Msg("[Mod] UIWaypointStandard.OnPointerEnter hooked");

                    if (Settings.useAnyWaypoint && ObjectManager.IsOfflineMode())
                        __instance.isActive = true;
                }
            }

            //todo: partially working. disabled until can polish (verify it still partially works in new update)
            [HarmonyPatch(typeof(GroundItemManager), "dropItemForPlayer", new Type[] { typeof(Actor), typeof(ItemData), typeof(Vector3), typeof(bool) })]
            public class GroundItemManager_vacuumNearbyStackableItems
            {
            	public static void Postfix(ref GroundItemManager __instance, ref int __state, ref Actor player, ref ItemData itemData, ref Vector3 location, ref bool playDropSound)
            	{
            		MelonLogger.Msg("[LEHud] GroundItemManager.dropItemForPlayer hooked");
            		if (ItemList.isCraftingItem(itemData.itemType) && Settings.pickupCrafting)
            		{
            			__instance.TryGetGroundItemList(player, out GroundItemList groundItemList);
            			__instance.vacuumNearbyStackableItems(player, groundItemList, location, StackableItemFlags.AllCrafting);
            		}
            	}
            }
            #endregion

            #region investigation hooks
            //[HarmonyPatch(typeof(DMMapIcon), "UpdateIcons")]
            //public class DMMapIconHooks
            //{
            //	//private static bool isFriendlyDotFound = false;
            //	//private static Image? friendlyDotImage = null;
            //	//private static Sprite? friendlyDotSprite = null;
            //
            //	//public static Image? FriendlyDotImage => friendlyDotImage;
            //	//public static Sprite? FriendlyDotSprite => friendlyDotSprite;
            //	private static void Postfix(DMMapIcon __instance)
            //	{
            //		//if (isFriendlyDotFound) return;
            //
            //		if (__instance != null)
            //		{
            //			//MelonLogger.Msg($"[Mod] DMMapIcon instance: {__instance.name}");
            //			//GameObject? gObject = __instance.gameObject;
            //			//Image? imageComponent = gObject.GetComponent<Image>();
            //			//Sprite? spriteComponent = gObject.GetComponent<Sprite>();
            //			//if (spriteComponent == null && imageComponent != null)
            //			//{
            //			//	MelonLogger.Msg("[Mod] DMMapIcon sprite is null. Trying to find in children.");
            //			//	spriteComponent = imageComponent.GetComponent<Sprite>();
            //			//}
            //			//if (imageComponent != null && spriteComponent != null && spriteComponent.name == "friendly-dot")
            //			//{
            //			//	friendlyDotImage = imageComponent;
            //			//	friendlyDotSprite = spriteComponent;
            //			//	isFriendlyDotFound = true;
            //
            //			//	MelonLogger.Msg($"[Mod] Found 'friendly-dot' with Image component. Storing reference.");
            //			//}
            //		}
            //		else
            //		{
            //			MelonLogger.Msg("[Mod] DMMapIcon.UpdateIcons instance is null.");
            //		}
            //	}
            //}

            //[HarmonyPatch(typeof(DMMapWorldIcon), "SetIcon")]
            //public class DMMapWorldIconHooks
            //{
            //	//private static void Prefix(DMMapWorldIcon __instance)
            //	//{
            //	//	if (__instance != null)
            //	//	{
            //	//		//MelonLogger.Msg($"[Mod] DMMapIconManager.SetIcon Prefix instance: {__instance.name}");
            //	//		MelonLogger.Msg($"[Mod] DMMapWorldIcon.SetIcon Prefix currentIcon: {__instance.currentIcon}");
            //	//		MelonLogger.Msg($"[Mod] DMMapWorldIcon.SetIcon Prefix IconType: {__instance.icon}");
            //	//	}
            //	//}
            //	private static void Postfix(DMMapWorldIcon __instance)
            //	{
            //		if (__instance != null)
            //		{
            //			//MelonLogger.Msg($"[Mod] DMMapIconManager.SetIcon Postfix instance: {__instance.name}");
            //
            //			//MelonLogger.Msg($"[Mod] DMMapWorldIcon.SetIcon Postfix currentIcon: {__instance.currentIcon}");
            //			//MelonLogger.Msg($"[Mod] DMMapWorldIcon.SetIcon Postfix IconType: {__instance.icon}");
            //		}
            //	}
            //}

            //[HarmonyPatch(typeof(DMMapIconManager), "Start")]
            //public class DMMapIconManagerHooks
            //{
            //	// the flow seems to be start from DMMapIconManager.Start -> BaseDMMapIcon.initialise to create minion icons on map
            //	private static void Prefix(DMMapIconManager __instance)
            //	{
            //		if (__instance != null)
            //		{
            //			//MelonLogger.Msg($"[Mod] DMMapIconManager.Start Prefix instance: {__instance.name}");
            //
            //			//MelonLogger.Msg($"[Mod] DMMapIconManager.Start Prefix currentIcon: {__instance.currentIcon}");
            //			//MelonLogger.Msg($"[Mod] DMMapIconManager.Start Prefix IconType: {__instance.icon}");
            //		}
            //	}
            //	private static void Postfix(DMMapIconManager __instance)
            //	{
            //		if (__instance != null)
            //		{
            //			//MelonLogger.Msg($"[Mod] DMMapIconManager.Start Postfix instance: {__instance.name}");
            //
            //			//MelonLogger.Msg($"[Mod] DMMapWorldIcon.Start Postfix currentIcon: {__instance.currentIcon}");
            //			//MelonLogger.Msg($"[Mod] DMMapWorldIcon.Start Postfix IconType: {__instance.icon}");
            //		}
            //	}
            //}

            //[HarmonyPatch(typeof(BaseDMMapIcon), nameof(BaseDMMapIcon.initialise))]
            //[HarmonyPostfix]
            //private static void initialisePostfix(BaseDMMapIcon __instance)
            //{
            //	if (__instance == null) return;
            //
            //	//MelonLogger.Msg($"[Mod] BaseDMMapIcon.initialise Postfix: {__instance.name}");
            //}

            //[HarmonyPatch(typeof(BaseDMMapIcon), "initialise")]
            //public class BaseDMMapIconInitHooks
            //{
            //	private static void Prefix(BaseDMMapIcon __instance)
            //	{
            //		if (__instance != null)
            //		{
            //			MelonLogger.Msg($"[Mod] BaseDMMapIcon.initialise Prefix instance: {__instance.name}");
            //
            //			//MelonLogger.Msg($"[Mod] DMMapIconManager.initialise Prefix currentIcon: {__instance.currentIcon}");
            //			//MelonLogger.Msg($"[Mod] DMMapIconManager.initialise Prefix IconType: {__instance.icon}");
            //		}
            //	}
            //	private static void Postfix(BaseDMMapIcon __instance)
            //	{
            //		if (__instance != null)
            //		{
            //			MelonLogger.Msg($"[Mod] BaseDMMapIcon.initialise Postfix instance: {__instance.name}");
            //
            //			//MelonLogger.Msg($"[Mod] DMMapWorldIcon.initialise Postfix currentIcon: {__instance.currentIcon}");
            //			//MelonLogger.Msg($"[Mod] DMMapWorldIcon.initialise Postfix IconType: {__instance.icon}");
            //		}
            //	}
            //}

            // this one fires every frame, we should avoid hooking into it unless necessary
            //[HarmonyPatch(typeof(BaseDMMapIcon), nameof(BaseDMMapIcon.UpdateIconSprite))]
            //[HarmonyPostfix]
            //private static void UpdateIconSpritePostfix(BaseDMMapIcon __instance)
            //{
            //	if (__instance == null) return;
            //
            //	//MelonLogger.Msg($"[Mod] BaseDMMapIcon.UpdateIconSprite: {__instance}");
            //}

            // this one has no names we can grab, even from the GO. unsure how useful it will be
            //[HarmonyPatch(typeof(BaseDMMapIcon), nameof(BaseDMMapIcon.UpdateIcons))]
            //[HarmonyPostfix]
            //private static void UpdateIconsPostfix(BaseDMMapIcon __instance)
            //{
            //	//if (__instance == null) return;
            //
            //	//MelonLogger.Msg($"[Mod] BaseDMMapIcon.UpdateIcons: {__instance.name}");
            //}
            #endregion

            #region user action monitoring patches
            // TODO: These patches need correct type names
            /*
			[HarmonyPatch(typeof(Il2CppLE.UI.InventoryPanel), "Open")]
			public class InventoryPanel_Open
			{
				private static void Postfix(Il2CppLE.UI.InventoryPanel __instance)
				{
					try
					{
						MelonLogger.Msg("[AntiIdle] Inventory panel opened - this might trigger anti-idle reset");
					}
					catch (Exception e)
					{
						MelonLogger.Error($"[Mod] InventoryPanel.Open Postfix error: {e.Message}");
					}
				}
			}

			[HarmonyPatch(typeof(Il2CppLE.UI.InventoryPanel), "Close")]
			public class InventoryPanel_Close
			{
				private static void Postfix(Il2CppLE.UI.InventoryPanel __instance)
				{
					try
					{
						MelonLogger.Msg("[AntiIdle] Inventory panel closed - this might trigger anti-idle reset");
					}
					catch (Exception e)
					{
						MelonLogger.Error($"[Mod] InventoryPanel.Close Postfix error: {e.Message}");
					}
				}
			}

			[HarmonyPatch(typeof(Il2CppLE.UI.CharacterPanel), "Open")]
			public class CharacterPanel_Open
			{
				private static void Postfix(Il2CppLE.UI.CharacterPanel __instance)
				{
					try
					{
						MelonLogger.Msg("[AntiIdle] Character panel opened - this might trigger anti-idle reset");
					}
					catch (Exception e)
					{
						MelonLogger.Error($"[Mod] CharacterPanel.Open Postfix error: {e.Message}");
					}
				}
			}

			[HarmonyPatch(typeof(Il2CppLE.UI.CharacterPanel), "Close")]
			public class CharacterPanel_Close
			{
				private static void Postfix(Il2CppLE.UI.CharacterPanel __instance)
				{
					try
					{
						MelonLogger.Msg("[AntiIdle] Character panel closed - this might trigger anti-idle reset");
					}
					catch (Exception e)
					{
						MelonLogger.Error($"[Mod] CharacterPanel.Close Postfix error: {e.Message}");
					}
				}
			}

			// Monitor player movement/input
			[HarmonyPatch(typeof(Il2CppLE.Player.LocalPlayer), "Update")]
			public class LocalPlayer_Update
			{
				private static void Postfix(Il2CppLE.Player.LocalPlayer __instance)
				{
					try
					{
						// Only log occasionally to avoid spam
						if (Time.frameCount % 300 == 0) // Every 300 frames (about 5 seconds at 60fps)
						{
							MelonLogger.Msg("[AntiIdle] Player update tick - checking for movement/input");
						}
					}
					catch (Exception e)
					{
						MelonLogger.Error($"[Mod] LocalPlayer.Update Postfix error: {e.Message}");
					}
				}
			}
			*/
            #endregion

            #region networking / anti-idle patches

            // NetPeer heartbeat hooks disabled: fields do not expose IL2CPP accessors
            /*
			[HarmonyPatch(typeof(Il2CppLidgren.Network.NetPeer), "get_m_lastHeartbeat")]
			public class NetPeer_LastHeartbeat_Get
			{
				private static void Postfix(Il2CppLidgren.Network.NetPeer __instance, double __result)
				{
					try
					{
						MelonLogger.Msg($"[Mod] NetPeer getter called: {__result}");
						if (__instance != null)
						{
							AntiIdleSystem.SetNetPeer(__instance);
							AntiIdleSystem.OnHeartbeatRead(__result);
						}
					}
					catch (Exception e)
					{
						MelonLogger.Error($"[Mod] NetPeer.m_lastHeartbeat getter Postfix error: {e.Message}");
					}
				}
			}

			[HarmonyPatch(typeof(Il2CppLidgren.Network.NetPeer), "set_m_lastHeartbeat")]
			public class NetPeer_LastHeartbeat_Set
			{
				private static void Prefix(Il2CppLidgren.Network.NetPeer __instance, double value)
				{
					try
					{
						MelonLogger.Msg($"[Mod] NetPeer setter called: {value}");
						if (__instance != null)
						{
							AntiIdleSystem.SetNetPeer(__instance);
							AntiIdleSystem.OnHeartbeatWrite(value);
						}
					}
					catch (Exception e)
					{
						MelonLogger.Error($"[Mod] NetPeer.m_lastHeartbeat setter Prefix error: {e.Message}");
					}
				}
			}
            */

            // These patches will hook into networking methods to detect and prevent idle timeouts
            [HarmonyPatch(typeof(NetMultiClient), "get_ConnectionStatus")]
            public class NetMultiClient_ConnectionStatus
            {
                private static void Postfix(NetMultiClient __instance, NetConnectionStatus __result)
                {
                    try
                    {
                        if (__instance != null)
                        {
                            // Update references
                            AntiIdleSystem.SetNetMultiClient(__instance);
                            AntiIdleSystem.OnConnectionStatusChanged(__result);

                            // Attempt to capture server connection from property if available
                            try
                            {
                                var type = __instance.GetType();
                                var prop = type.GetProperty("ServerConnection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                if (prop != null)
                                {
                                    var conn = prop.GetValue(__instance);
                                    if (conn != null)
                                        AntiIdleSystem.SetServerConnection(conn);
                                }
                                else
                                {
                                    // Fallbacks: try common collections: Connections, m_connections, connection, m_connection
                                    object? candidate = null;

                                    // 1) Properties that look like collections of connections
                                    var props = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                    foreach (var p in props)
                                    {
                                        var n = p.Name.ToLowerInvariant();
                                        if (n.Contains("connection"))
                                        {
                                            try
                                            {
                                                var val = p.GetValue(__instance);
                                                candidate = TryPickSingleConnection(val) ?? candidate;
                                                if (candidate != null) break;
                                            }
                                            catch { }
                                        }
                                    }

                                    // 2) Fields that look like collections of connections
                                    if (candidate == null)
                                    {
                                        var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                        foreach (var f in fields)
                                        {
                                            var n = f.Name.ToLowerInvariant();
                                            if (n.Contains("connection"))
                                            {
                                                try
                                                {
                                                    var val = f.GetValue(__instance);
                                                    candidate = TryPickSingleConnection(val) ?? candidate;
                                                    if (candidate != null) break;
                                                }
                                                catch { }
                                            }
                                        }
                                    }

                                    if (candidate != null)
                                    {
                                        AntiIdleSystem.SetServerConnection(candidate);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  NetMultiClient.ConnectionStatus Postfix error: {e.Message}");
                    }
                }

                // Try to pick a single NetConnection from various collection shapes
                private static object? TryPickSingleConnection(object? value)
                {
                    if (value == null) return null;
                    try
                    {
                        var valType = value.GetType();

                        // If it's already a NetConnection, return it
                        if (valType.Name.Contains("NetConnection"))
                            return value;

                        // Handle Il2Cpp arrays
                        if (valType.IsArray)
                        {
                            var arr = value as System.Array;
                            if (arr != null && arr.Length == 1)
                                return arr.GetValue(0);
                            if (arr != null && arr.Length > 0)
                                return arr.GetValue(0); // pick first as best-effort
                        }

                        // Handle generic collections (List<NetConnection>, etc.)
                        var countProp = valType.GetProperty("Count") ?? valType.GetProperty("Length");
                        if (countProp != null)
                        {
                            var countObj = countProp.GetValue(value);
                            int count = 0;
                            try { count = Convert.ToInt32(countObj); } catch { }
                            if (count > 0)
                            {
                                var indexer = valType.GetMethod("get_Item");
                                if (indexer != null)
                                    return indexer.Invoke(value, new object[] { 0 });

                                // Fallback: enumerate via IEnumerable
                                var enumerable = value as System.Collections.IEnumerable;
                                if (enumerable != null)
                                {
                                    foreach (var item in enumerable)
                                        return item; // first
                                }
                            }
                        }

                        // Handle single field/property named similarly to a NetConnection
                        foreach (var p in valType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                        {
                            if (p.PropertyType.Name.Contains("NetConnection"))
                                return p.GetValue(value);
                        }
                        foreach (var f in valType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                        {
                            if (f.FieldType.Name.Contains("NetConnection"))
                                return f.GetValue(value);
                        }
                    }
                    catch { }
                    return null;
                }
            }

			[HarmonyPatch(typeof(NetMultiClient), "Connect", new Type[] { typeof(IPEndPoint), typeof(NetOutgoingMessage) })]
			public class NetMultiClient_Connect
			{
				private static void Prefix(NetMultiClient __instance)
				{
					try
					{
						// Reduced: no verbose prefix log
					}
					catch (Exception e)
					{
						MelonLogger.Error($"[LeHud.Hooks]  NetMultiClient.Connect Prefix error: {e.Message}");
					}
				}

				private static void Postfix(NetMultiClient __instance, NetConnection __result)
				// private static void Postfix(Il2CppLidgren.Network.NetMultiClient __instance, Il2CppLidgren.Network.NetConnection __result)
				{
					try
					{
						if (__result != null)
						{
							AntiIdleSystem.SetServerConnection(__result);
							AntiIdleSystem.SetNetMultiClient(__instance);
						}
					}
					catch (Exception e)
					{
						MelonLogger.Error($"[LeHud.Hooks]  NetMultiClient.Connect Postfix error: {e.Message}");
					}
				}
			}

            [HarmonyPatch(typeof(NetMultiClient), "Disconnect")]
            public class NetMultiClient_Disconnect
            {
                private static void Prefix(NetMultiClient __instance, string byeMessage)
                {
                    try
                    {
                        MelonLogger.Msg($"[LeHud.Hooks]  NetMultiClient.Disconnect Prefix - Reason: {(byeMessage ?? "No reason provided")}");

                        // Log disconnect attempt for anti-idle analysis
                        AntiIdleSystem.OnDisconnectAttempted(byeMessage);
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  NetMultiClient.Disconnect Prefix error: {e.Message}");
                    }
                }

                private static void Postfix(NetMultiClient __instance, string byeMessage)
                {
                    try
                    {
                        MelonLogger.Msg($"[LeHud.Hooks]  NetMultiClient.Disconnect Postfix - Disconnection completed");

                        // Clear stored references
                        AntiIdleSystem.ClearConnections();
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  NetMultiClient.Disconnect Postfix error: {e.Message}");
                    }
                }
            }

            [HarmonyPatch(typeof(NetMultiClient), "SendMessage")]
            public class NetMultiClient_SendMessage
            {
                private static void Prefix(NetMultiClient __instance, NetOutgoingMessage msg, NetDeliveryMethod method, int sequenceChannel)
                {
                    try
                    {

                        // Track message sending for anti-idle analysis
                        AntiIdleSystem.OnMessageSent(msg, method, sequenceChannel);
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  NetMultiClient.SendMessage Prefix error: {e.Message}");
                    }
                }

                private static void Postfix(NetMultiClient __instance, NetOutgoingMessage msg, NetDeliveryMethod method, int sequenceChannel, NetSendResult __result)
                {
                    try
                    {
                        // Reduced: no per-message logging
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  NetMultiClient.SendMessage Postfix error: {e.Message}");
                    }
                }
            }

            // Hook into individual NetConnection status changes
            [HarmonyPatch(typeof(NetConnection), "get_Status")]
            public class NetConnection_Status
            {
                private static void Postfix(NetConnection __instance, NetConnectionStatus __result)
                {
                    try
                    {
                        if (__instance != null)
                        {
                            // Track individual connection status (reduced logging)
                            AntiIdleSystem.OnNetConnectionStatusChanged(__instance, __result);
                        }
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  NetConnection.Status Postfix error: {e.Message}");
                    }
                }
            }

            // Hook into Steam networking callbacks if available
            [HarmonyPatch(typeof(SteamNetworkingSockets), "ConnectionStatusChanged")]
            public class SteamNetworking_ConnectionStatusChanged
            {
                private static void Prefix(SteamNetworkingSockets __instance, Il2CppSteamworks.Data.SteamNetConnectionStatusChangedCallback_t data)
                {
                    try
                    {
                        MelonLogger.Msg($"[LeHud.Hooks]  SteamNetworking.ConnectionStatusChanged Prefix - Callback data received");

                        // Track Steam networking status changes
                        AntiIdleSystem.OnSteamConnectionStatusChanged(data);
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error($"[LeHud.Hooks]  SteamNetworking.ConnectionStatusChanged Prefix error: {e.Message}");
                    }
                }
            }
            #endregion
        }
    }
}
