using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppItemFiltering;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace Mod.Game
{
	internal class ItemFiltering
	{
		// You have to manually call this method from a bug with Il2cppSystem.Nullable<T>1 in the generated code
	// 	[CallerCount(3)]
    //     [CachedScanResults(RefRangeStart = 1048227, RefRangeEnd = 1048230, XrefRangeStart = 1048208, XrefRangeEnd = 1048227, MetadataInitTokenRva = 0, MetadataInitFlagRva = 0)]
    //     public unsafe Rule.RuleOutcome Match(
    // ItemDataUnpacked itemData,
    // out Il2CppSystem.Nullable<int> color,
    // out Il2CppSystem.Nullable<bool> emphasize,
    // out int matchingRuleNumber,
    // out int soundId,
    // out int mapIconId,
    // out bool beamOverride,
    // out EpochColor.BeamSize beamSize,
    // out Color beamColor)
    //     {
    //         IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)this);
    //         System.IntPtr* numPtr1 = stackalloc System.IntPtr[9];
    //         numPtr1[0] = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)itemData);
    //         System.IntPtr num1 = (System.IntPtr)numPtr1 + checked(new System.IntPtr(1) * sizeof(System.IntPtr));
    //         System.IntPtr zero1 = System.IntPtr.Zero;
    //         System.IntPtr* numPtr2 = &zero1;
    //         *(System.IntPtr*)num1 = (System.IntPtr)numPtr2;
    //         System.IntPtr num2 = (System.IntPtr)numPtr1 + checked(new System.IntPtr(2) * sizeof(System.IntPtr));
    //         System.IntPtr zero2 = System.IntPtr.Zero;
    //         System.IntPtr* numPtr3 = &zero2;
    //         *(System.IntPtr*)num2 = (System.IntPtr)numPtr3;
    //         *(System.IntPtr*)((System.IntPtr)numPtr1 + checked(new System.IntPtr(3) * sizeof(System.IntPtr))) = (System.IntPtr)ref matchingRuleNumber;
    //         *(System.IntPtr*)((System.IntPtr)numPtr1 + checked(new System.IntPtr(4) * sizeof(System.IntPtr))) = (System.IntPtr)ref soundId;
    //         *(System.IntPtr*)((System.IntPtr)numPtr1 + checked(new System.IntPtr(5) * sizeof(System.IntPtr))) = (System.IntPtr)ref mapIconId;
    //         *(System.IntPtr*)((System.IntPtr)numPtr1 + checked(new System.IntPtr(6) * sizeof(System.IntPtr))) = (System.IntPtr)ref beamOverride;
    //         *(System.IntPtr*)((System.IntPtr)numPtr1 + checked(new System.IntPtr(7) * sizeof(System.IntPtr))) = (System.IntPtr)ref beamSize;
    //         *(System.IntPtr*)((System.IntPtr)numPtr1 + checked(new System.IntPtr(8) * sizeof(System.IntPtr))) = (System.IntPtr)ref beamColor;
    //         System.IntPtr num3;
    //         System.IntPtr num4 = IL2CPP.il2cpp_runtime_invoke(ItemFilter.NativeMethodInfoPtr_Match_Public_RuleOutcome_ItemDataUnpacked_byref_Nullable_1_Int32_byref_Nullable_1_Boolean_byref_Int32_byref_Int32_byref_Int32_byref_Boolean_byref_BeamSize_byref_Color_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)this), (void**)numPtr1, ref num3);
    //         Il2CppException.RaiseExceptionIfNecessary(num3);
    //         ref Il2CppSystem.Nullable<int> local1 = ref color;
    //         System.IntPtr pointer1 = zero1;
    //         Il2CppSystem.Nullable<int> nullable1 = pointer1 == System.IntPtr.Zero ? (Il2CppSystem.Nullable<int>)null : new Il2CppSystem.Nullable<int>(pointer1);
    //         local1 = nullable1;
    //         ref Il2CppSystem.Nullable<bool> local2 = ref emphasize;
    //         System.IntPtr pointer2 = zero2;
    //         Il2CppSystem.Nullable<bool> nullable2 = pointer2 == System.IntPtr.Zero ? (Il2CppSystem.Nullable<bool>)null : new Il2CppSystem.Nullable<bool>(pointer2);
    //         local2 = nullable2;
    //         return *(Rule.RuleOutcome*)IL2CPP.il2cpp_object_unbox(num4);
    //     }

		private static float s_nextNullFilterReminderAt;
		private static int s_nullFilterRemindersShown;
		private static IntPtr s_matchMethodPtr;
		private static bool s_hasResolvedMatchMethod;
		private static bool s_loggedMissingMatchMethod;
		private const int MaxNullFilterReminders = 3;
		private const float NullFilterReminderIntervalSeconds = 5f;
		private const string ExtendedMatchMethodFieldName = "NativeMethodInfoPtr_Match_Public_RuleOutcome_ItemDataUnpacked_byref_Nullable_1_Int32_byref_Nullable_1_Boolean_byref_Int32_byref_Int32_byref_Int32_byref_Boolean_byref_BeamSize_byref_Color_0";

		private static void MaybeLogNoFilterSelected()
		{
			float now = Time.realtimeSinceStartup;
			if (s_nullFilterRemindersShown >= MaxNullFilterReminders)
				return;

			if (now < s_nextNullFilterReminderAt)
				return;

			s_nextNullFilterReminderAt = now + NullFilterReminderIntervalSeconds;
			s_nullFilterRemindersShown++;
			MelonLogger.Warning("ItemFiltering: No item filter is selected. Open the in-game Item Filter menu and select or create a filter. Suppressing further messages temporarily.");
		}

		private static void ResetNoFilterWarning()
		{
			s_nextNullFilterReminderAt = 0f;
			s_nullFilterRemindersShown = 0;
		}

		private static bool TryResolveMatchMethod()
		{
			if (s_hasResolvedMatchMethod)
			{
				return s_matchMethodPtr != IntPtr.Zero;
			}

			s_hasResolvedMatchMethod = true;
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;

			FieldInfo? extendedField = typeof(ItemFilter).GetField(ExtendedMatchMethodFieldName, flags);
			if (extendedField?.GetValue(null) is IntPtr extendedPtr && extendedPtr != IntPtr.Zero)
			{
				s_matchMethodPtr = extendedPtr;
				MelonLogger.Msg("[ItemFiltering] Using ItemFilter.Match signature (9 args)");
				return true;
			}

			s_matchMethodPtr = IntPtr.Zero;
			return false;
		}

		public unsafe static Rule.RuleOutcome Match(ItemDataUnpacked itemData, Il2CppSystem.Nullable<int>? color, Il2CppSystem.Nullable<bool>? emphasize, int matchingRuleNumber, int soundId, int mapIconId)
		{
			// Note: parameters are not out/ref, so assignments here will not propagate to caller.
			// We still pass proper pointers to IL2CPP to satisfy both signature variants.
			color = null;
			emphasize = null;
			matchingRuleNumber = 0;
			soundId = 0;
			mapIconId = 0;

			var itemFilter = ItemFilterManager.Instance.Filter;
			if (itemFilter == null)
			{
				MaybeLogNoFilterSelected();
				return Rule.RuleOutcome.SHOW;
			}

			ResetNoFilterWarning();

			if (!TryResolveMatchMethod())
			{
				if (!s_loggedMissingMatchMethod)
				{
					s_loggedMissingMatchMethod = true;
					MelonLogger.Error("[ItemFiltering] ItemFilter.Match native method pointer not found");
				}
				return Rule.RuleOutcome.SHOW;
			}

			// Out Nullable pointers are returned via IntPtr indirection.
			IntPtr colorPtr = IntPtr.Zero;
			IntPtr emphasizePtr = IntPtr.Zero;
			int ruleNum = 0;

			IntPtr exc = IntPtr.Zero;
			bool beamOverride = false;
			int beamSize = 0; // EpochColor.BeamSize underlying enum value
			Color beamColor = default;

			IntPtr* args = stackalloc IntPtr[9];
			args[0] = IL2CPP.Il2CppObjectBaseToPtr(itemData);
			args[1] = (IntPtr)(&colorPtr);
			args[2] = (IntPtr)(&emphasizePtr);
			args[3] = (IntPtr)(&ruleNum);
			args[4] = (IntPtr)(&soundId);
			args[5] = (IntPtr)(&mapIconId);
			args[6] = (IntPtr)(&beamOverride);
			args[7] = (IntPtr)(&beamSize);
			args[8] = (IntPtr)(&beamColor);

			IntPtr result = IL2CPP.il2cpp_runtime_invoke(s_matchMethodPtr, IL2CPP.Il2CppObjectBaseToPtrNotNull(itemFilter), (void**)args, ref exc);

			if (exc != IntPtr.Zero)
			{
				Il2CppException.RaiseExceptionIfNecessary(exc);
				return Rule.RuleOutcome.SHOW;
			}

			// Rehydrate Nullable<T> from returned pointers (local only; not observable by caller without out/ref)
			color = colorPtr != IntPtr.Zero ? new Il2CppSystem.Nullable<int>(colorPtr) : null;
			emphasize = emphasizePtr != IntPtr.Zero ? new Il2CppSystem.Nullable<bool>(emphasizePtr) : null;
			matchingRuleNumber = ruleNum;

			return *(Rule.RuleOutcome*)IL2CPP.il2cpp_object_unbox(result);
		}

		public unsafe static Rule.RuleOutcome Match(ItemDataUnpacked itemData, Il2CppSystem.Nullable<int>? color, Il2CppSystem.Nullable<bool>? emphasize, int matchingRuleNumber)
		{
			int soundId = 0;
			int mapIconId = 0;
			return Match(itemData, color, emphasize, matchingRuleNumber, soundId, mapIconId);
		}
	}
}
