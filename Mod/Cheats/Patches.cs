using UnityEngine;
using MelonLoader;
using Il2Cpp;
using Il2CppDMM;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppLE.UI;
using Il2CppLE.Telemetry;
using Il2CppItemFiltering;
using HarmonyPatch = HarmonyLib.HarmonyPatch;
using static MelonLoader.LoaderConfig;
using static Il2Cpp.GroundItemManager;
using Mod.Utils;


namespace Mod.Cheats.Patches
{
    public class MapIconPatch : MonoBehaviour
    {
        private bool isInitialized = false;
        private static string Icon_Base64 = SpriteBases.npcMapIcon;

        [HarmonyPatch(typeof(ActorSync), nameof(ActorSync.ReceiveInitDisplayInformation))]
        private static class ActorSync_MessageSyncRarit
        {
            private static void Postfix(ActorSync __instance, byte rarity)
            {
                try
                {
                    
                }
                catch (Exception e)
                {

                }
            }
        }
    }

    [HarmonyPatch]
    internal class HarmonyPatches
    {
        // TODO: implement patches to guard against accidental self reports of modifying the game
        // patch out the SendBugToServer() function
        // il2CppLE->il2Cpp->il2CppLELE.Utility->ClickUp->SubmitNewBugReport()
        // should also patch out the crash handler if possible

        #region security / detection patches
        [HarmonyPatch(typeof(UIBase), "Awake")]
        public class UIBase_Awake : MelonMod
        {
            public static void Prefix(ref UIBase __instance)
            {
                MelonLogger.Msg("[Mod] UIBase.Awake hooked. Disabling bug submission button");
                //__instance.gameObject.SetActive(false);
                __instance.bugReportButton.gameObject.SetActive(false);
                __instance.bugReportPanel.Close();
            }
        }

        [HarmonyPatch(typeof(CharacterSelect), "Awake")]
        public class CharacterSelect_ : MelonMod
        {
            public static void Prefix(ref CharacterSelect __instance)
            {
                MelonLogger.Msg("[Mod] CharacterSelect.Awake hooked. Disabling bug submission button");
                __instance.submitBugReportButton.gameObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(UIBase), "OpenBugReportPanel")]
        public class UIBase_OpenBugReportPanel : MelonMod
        {
            public static bool Prefix(ref UIBase __instance)
            {
                
                MelonLogger.Msg("[Mod] UIBase.OpenBugReportPanel hooked and blocked.");
                //__instance.gameObject.SetActive(false);
                __instance.bugReportButton.gameObject.SetActive(false);
                __instance.bugReportPanel.Close();
                return false;
            }
        }

        [HarmonyPatch(typeof(BugSubmitter), "Submit")]
        public class BugSubmitter_Submit : MelonMod
        {
            public static bool Prefix(ref BugSubmitter __instance)
            {
                MelonLogger.Msg("[Mod] BugSubmitter.Submit hooked and blocked.");
                __instance.gameObject.gameObject.SetActive(false);
                return false;
            }
        }

        [HarmonyPatch(typeof(BugSubmitter), "ShowSubmitPanel")]
        public class BugSubmitter_ShowSubmitPanel : MelonMod
        {
            public static bool Prefix(ref BugSubmitter __instance)
            {
                MelonLogger.Msg("[Mod] BugSubmitter.ShowSubmitPanel hooked and blocked.");
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
                //    for (int i = 0; i < args.Length; i++)
                //    {
                //        MelonLogger.Msg($"Arg[{i}]: {args[i]?.ToString() ?? "null"}");
                //    }
                //}
                //else
                //{
                //    MelonLogger.Msg("Args: null");
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
                MelonLogger.Msg("[Mod] ClientLogHandler.LogException hooked and blocked.");

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
                MelonLogger.Msg("[Mod] AccountSupport_GetLogsZip hooked and blocked.");
                return false;
            }
        }

        //[HarmonyPatch(typeof(CharacterSelectPanelUI), "OpenBugReport")]
        //public class CharacterSelectPanelUI_OpenBugReport : MelonMod
        //{
        //    public static void Prefix(ref CharacterSelectPanelUI __instance)
        //    {
        //        MelonLogger.Msg("[Mod] CharacterSelectPanelUI.OpenBugReport hooked. Disabling bug submission");
        //        //__instance.gameObject.SetActive(false);
        //        return;
        //    }
        //}

        //[HarmonyPatch(typeof(LandingZonePanel), "OnAwake")]
        //public class LandingZonePanel_ : MelonMod
        //{
        //    public static void Prefix(ref LandingZonePanel __instance)
        //    {
        //        MelonLogger.Msg("[Mod] LandingZonePanel.OnAwake hooked");
        //    }
        //}
        #endregion

        #region active game patches
        [HarmonyPatch]
        [HarmonyPatch(typeof(CameraManager), "ApplyZoom")]
        public class Camera_ : MelonMod
        {
            //todo: getting kicked due to idle can cause this to stop triggering somehow
            //todo: also appears to not work in offline mode (low prio). probably uses a diff camera manager
            //todo: or maybe it just breaks when switching to offline at char select
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
                    MelonLogger.Msg("[Mod] Camera max zoom patched (3x)");
                    // zoomDefault: -17.5
                    // zoomMin: -7
                }
                else if (isPatched && !Settings.cameraZoomUnlock)
                {
                    //MelonLogger.Msg("[Mod] CameraManager unhooked");
                    __instance.zoomDefault = -17.5f;
                    isPatched = false;
                    MelonLogger.Msg("[Mod] Camera max zoom unpatched (1x)");
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
                    MelonLogger.Msg("[Mod] minimap max zoom patched ()");
                    // zoomdefault: 37.5
                }
                else if (isPatched && !Settings.minimapZoomUnlock)
                {
                    //MelonLogger.Msg("[Mod] DMMapZoom unhooked");
                    __instance.maxMinimapZoom = 37.5f;
                    isPatched = false;
                    MelonLogger.Msg("[Mod] minimap max zoom unpatched (1x)");
                }
            }
        }
        #endregion

        #region risky game patches
        [HarmonyPatch(typeof(UIWaypointStandard), "OnPointerEnter", new Type[] { typeof(UnityEngine.EventSystems.PointerEventData) })]
        internal class WayPointUnlock
        {
            public static void Prefix(UIWaypointStandard __instance, UnityEngine.EventSystems.PointerEventData eventData)
            {
                //MelonLogger.Msg("[Mod] UIWaypointStandard.OnPointerEnter hooked");

                if (Settings.useAnyWaypoint)
                    __instance.isActive = true;
            }
        }

        //todo: partially working. disabled until can polish
        //[HarmonyPatch(typeof(GroundItemManager), "dropItemForPlayer", new Type[] { typeof(Actor), typeof(ItemData), typeof(Vector3), typeof(bool) })]
        //public class GroundItemManager_vacuumNearbyStackableItems
        //{
        //    public static void Postfix(ref GroundItemManager __instance, ref int __state, ref Actor player, ref ItemData itemData, ref Vector3 location, ref bool playDropSound)
        //    {
        //        MelonLogger.Msg("[Mod] GroundItemManager.dropItemForPlayer hooked");
        //        if (ItemList.isCraftingItem(itemData.itemType) && Settings.pickupCrafting)
        //        {
        //            __instance.TryGetGroundItemList(player, out GroundItemList groundItemList);
        //            __instance.vacuumNearbyStackableItems(player, groundItemList, location, StackableItemFlags.AllCrafting);
        //        }
        //    }
        //}
        #endregion

        #region investigation hooks
        //[HarmonyPatch(typeof(DMMapIcon), "UpdateIcons")]
        //public class DMMapIconHooks
        //{
        //    //private static bool isFriendlyDotFound = false;
        //    //private static Image? friendlyDotImage = null;
        //    //private static Sprite? friendlyDotSprite = null;

        //    //public static Image? FriendlyDotImage => friendlyDotImage;
        //    //public static Sprite? FriendlyDotSprite => friendlyDotSprite;
        //    private static void Postfix(DMMapIcon __instance)
        //    {
        //        //if (isFriendlyDotFound) return;

        //        if (__instance != null)
        //        {
        //            //MelonLogger.Msg($"[Mod] DMMapIcon instance: {__instance.name}");
        //            //GameObject? gObject = __instance.gameObject;
        //            //Image? imageComponent = gObject.GetComponent<Image>();
        //            //Sprite? spriteComponent = gObject.GetComponent<Sprite>();
        //            //if (spriteComponent == null && imageComponent != null)
        //            //{
        //            //    MelonLogger.Msg("[Mod] DMMapIcon sprite is null. Trying to find in children.");
        //            //    spriteComponent = imageComponent.GetComponent<Sprite>();
        //            //}
        //            //if (imageComponent != null && spriteComponent != null && spriteComponent.name == "friendly-dot")
        //            //{
        //            //    friendlyDotImage = imageComponent;
        //            //    friendlyDotSprite = spriteComponent;
        //            //    isFriendlyDotFound = true;

        //            //    MelonLogger.Msg($"[Mod] Found 'friendly-dot' with Image component. Storing reference.");
        //            //}
        //        }
        //        else
        //        {
        //            MelonLogger.Msg("[Mod] DMMapIcon.UpdateIcons instance is null.");
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(DMMapWorldIcon), "SetIcon")]
        //public class DMMapWorldIconHooks
        //{
        //    //private static void Prefix(DMMapWorldIcon __instance)
        //    //{
        //    //    if (__instance != null)
        //    //    {
        //    //        //MelonLogger.Msg($"[Mod] DMMapIconManager.SetIcon Prefix instance: {__instance.name}");
        //    //        MelonLogger.Msg($"[Mod] DMMapWorldIcon.SetIcon Prefix currentIcon: {__instance.currentIcon}");
        //    //        MelonLogger.Msg($"[Mod] DMMapWorldIcon.SetIcon Prefix IconType: {__instance.icon}");
        //    //    }
        //    //}
        //    private static void Postfix(DMMapWorldIcon __instance)
        //    {
        //        if (__instance != null)
        //        {
        //            //MelonLogger.Msg($"[Mod] DMMapIconManager.SetIcon Postfix instance: {__instance.name}");

        //            //MelonLogger.Msg($"[Mod] DMMapWorldIcon.SetIcon Postfix currentIcon: {__instance.currentIcon}");
        //            //MelonLogger.Msg($"[Mod] DMMapWorldIcon.SetIcon Postfix IconType: {__instance.icon}");
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(DMMapIconManager), "Start")]
        //public class DMMapIconManagerHooks
        //{
        //    // the flow seems to be start from DMMapIconManager.Start -> BaseDMMapIcon.initialise to create minion icons on map
        //    private static void Prefix(DMMapIconManager __instance)
        //    {
        //        if (__instance != null)
        //        {
        //            //MelonLogger.Msg($"[Mod] DMMapIconManager.Start Prefix instance: {__instance.name}");

        //            //MelonLogger.Msg($"[Mod] DMMapIconManager.Start Prefix currentIcon: {__instance.currentIcon}");
        //            //MelonLogger.Msg($"[Mod] DMMapIconManager.Start Prefix IconType: {__instance.icon}");
        //        }
        //    }
        //    private static void Postfix(DMMapIconManager __instance)
        //    {
        //        if (__instance != null)
        //        {
        //            //MelonLogger.Msg($"[Mod] DMMapIconManager.Start Postfix instance: {__instance.name}");

        //            //MelonLogger.Msg($"[Mod] DMMapWorldIcon.Start Postfix currentIcon: {__instance.currentIcon}");
        //            //MelonLogger.Msg($"[Mod] DMMapWorldIcon.Start Postfix IconType: {__instance.icon}");
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(BaseDMMapIcon), nameof(BaseDMMapIcon.initialise))]
        //[HarmonyPostfix]
        //private static void initialisePostfix(BaseDMMapIcon __instance)
        //{
        //    if (__instance == null) return;

        //    //MelonLogger.Msg($"[Mod] BaseDMMapIcon.initialise Postfix: {__instance.name}");
        //}

        //[HarmonyPatch(typeof(BaseDMMapIcon), "initialise")]
        //public class BaseDMMapIconInitHooks
        //{
        //    private static void Prefix(BaseDMMapIcon __instance)
        //    {
        //        if (__instance != null)
        //        {
        //            MelonLogger.Msg($"[Mod] BaseDMMapIcon.initialise Prefix instance: {__instance.name}");

        //            //MelonLogger.Msg($"[Mod] DMMapIconManager.initialise Prefix currentIcon: {__instance.currentIcon}");
        //            //MelonLogger.Msg($"[Mod] DMMapIconManager.initialise Prefix IconType: {__instance.icon}");
        //        }
        //    }
        //    private static void Postfix(BaseDMMapIcon __instance)
        //    {
        //        if (__instance != null)
        //        {
        //            MelonLogger.Msg($"[Mod] BaseDMMapIcon.initialise Postfix instance: {__instance.name}");

        //            //MelonLogger.Msg($"[Mod] DMMapWorldIcon.initialise Postfix currentIcon: {__instance.currentIcon}");
        //            //MelonLogger.Msg($"[Mod] DMMapWorldIcon.initialise Postfix IconType: {__instance.icon}");
        //        }
        //    }
        //}

        // this one fires every frame, we should avoid hooking into it unless necessary
        //[HarmonyPatch(typeof(BaseDMMapIcon), nameof(BaseDMMapIcon.UpdateIconSprite))]
        //[HarmonyPostfix]
        //private static void UpdateIconSpritePostfix(BaseDMMapIcon __instance)
        //{
        //    if (__instance == null) return;

        //    //MelonLogger.Msg($"[Mod] BaseDMMapIcon.UpdateIconSprite: {__instance}");
        //}

        // this one has no names we can grab, even from the GO. unsure how useful it will be
        //[HarmonyPatch(typeof(BaseDMMapIcon), nameof(BaseDMMapIcon.UpdateIcons))]
        //[HarmonyPostfix]
        //private static void UpdateIconsPostfix(BaseDMMapIcon __instance)
        //{
        //    //if (__instance == null) return;

        //    //MelonLogger.Msg($"[Mod] BaseDMMapIcon.UpdateIcons: {__instance.name}");
        //}
        #endregion
    }
}
