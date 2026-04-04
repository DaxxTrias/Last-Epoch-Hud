using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Il2Cpp;
using MelonLoader;
using Mod.Game;
using UnityEngine;

namespace Mod.Cheats.ESP
{
	internal static class Barrels
	{
		private const string BarrelAlignmentName = "Barrel";
		private const string BarrelLabel = "Barrel";
		private static readonly Color BarrelColor = Drawing.AlignmentToColor(BarrelAlignmentName);

		private static bool s_reflectionInitAttempted;
		private static bool s_reflectionReady;
		private static bool s_loggedReflectionFailure;
		private static bool s_loggedAlignmentFallback;

		private static MemberInfo? s_actorBucketsMember;
		private static MemberInfo? s_actorsMember;
		private static MemberInfo? s_dlistBackingListMember;
		private static MethodInfo? s_getAlignmentMethod;

		private static readonly string[] ActorBucketsMemberNames =
		{
			"actors",
			"Actors",
			"ActorList",
			"actorList",
			"actorLists",
			"ActorLists"
		};

		private static readonly string[] ActorsMemberNames = { "actors", "Actors" };
		private static readonly string[] DListBackingNames = { "_list", "list", "List" };

		public static void OnUpdate()
		{
			if (!ObjectManager.HasPlayer()) return;
			if (!Settings.ShouldDrawNPCAlignment(BarrelAlignmentName)) return;

			var localPlayer = ObjectManager.GetLocalPlayer();
			if (localPlayer == null) return;
			var localPos = localPlayer.transform.position;
			float maxDistance = Settings.drawDistance;

			if (!EnsureReflectionBindings()) return;
			if (ActorManager.instance == null) return;

			var actorBucketsObj = GetMemberValue(s_actorBucketsMember!, ActorManager.instance);
			if (actorBucketsObj == null) return;

			foreach (var bucket in EnumerateObjects(actorBucketsObj))
			{
				if (bucket == null) continue;

				var actorsObj = GetMemberValue(s_actorsMember!, bucket);
				if (actorsObj == null) continue;

				foreach (var entry in EnumerateObjects(actorsObj))
				{
					if (entry is not Actor actor) continue;
					if (actor.gameObject == null || !actor.gameObject.activeInHierarchy) continue;

					if (!IsBarrel(actor)) continue;

					var actorPos = actor.transform.position;
					if (Vector3.Distance(localPos, actorPos) > maxDistance) continue;

					var labelPos = actorPos;
					labelPos.y += 1.1f;

					if (Settings.showESPLines) ESP.AddLine(localPos, actorPos, BarrelColor);
					if (Settings.showESPLabels) ESP.AddString(BarrelLabel, labelPos, BarrelColor);
				}
			}
		}

		private static bool EnsureReflectionBindings()
		{
			if (s_reflectionInitAttempted) return s_reflectionReady;
			s_reflectionInitAttempted = true;

			try
			{
				s_actorBucketsMember = FindMember(typeof(ActorManager), ActorBucketsMemberNames);
				s_getAlignmentMethod = typeof(Actor).GetMethod("GetAlignment", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
				if (s_actorBucketsMember == null || s_getAlignmentMethod == null)
				{
					LogReflectionFailureOnce("ActorManager actors/GetAlignment binding failed.");
					s_reflectionReady = false;
					return false;
				}

				var actorBucketsType = GetMemberType(s_actorBucketsMember);
				if (actorBucketsType == null)
				{
					LogReflectionFailureOnce("ActorManager actors member type could not be resolved.");
					s_reflectionReady = false;
					return false;
				}

				var actorListType = ResolveCollectionElementType(actorBucketsType) ?? actorBucketsType;
				s_actorsMember = FindMember(actorListType, ActorsMemberNames);
				if (s_actorsMember == null)
				{
					LogReflectionFailureOnce("ActorList.actors binding failed.");
					s_reflectionReady = false;
					return false;
				}

				var dListType = GetMemberType(s_actorsMember);
				if (dListType == null)
				{
					LogReflectionFailureOnce("ActorList.actors type could not be resolved.");
					s_reflectionReady = false;
					return false;
				}

				s_dlistBackingListMember = FindMember(dListType, DListBackingNames);
				if (s_dlistBackingListMember == null)
				{
					LogReflectionFailureOnce("DList backing list member binding failed.");
					s_reflectionReady = false;
					return false;
				}

				s_reflectionReady = true;
				return true;
			}
			catch (Exception e)
			{
				LogReflectionFailureOnce("Reflection binding exception: " + e.Message);
				s_reflectionReady = false;
				return false;
			}
		}

		private static bool IsBarrel(Actor actor)
		{
			var alignment = GetAlignmentName(actor);
			if (string.Equals(alignment, BarrelAlignmentName, StringComparison.Ordinal))
			{
				return true;
			}

			var goName = actor.gameObject?.name;
			if (string.IsNullOrEmpty(goName)) return false;

			// Fallback for builds where alignment is unavailable on raw Actor entries.
			return goName.IndexOf("Breakable_Barrel_2019", StringComparison.OrdinalIgnoreCase) >= 0
				|| goName.IndexOf("Barrel", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static string GetAlignmentName(Actor actor)
		{
			if (s_getAlignmentMethod == null)
			{
				LogAlignmentFallbackOnce("GetAlignment method unavailable; using name fallback.");
				return string.Empty;
			}

			try
			{
				var value = s_getAlignmentMethod.Invoke(actor, null);
				return value?.ToString() ?? string.Empty;
			}
			catch
			{
				LogAlignmentFallbackOnce("GetAlignment invocation failed; using name fallback.");
				return string.Empty;
			}
		}

		private static MemberInfo? FindMember(Type type, string[] names)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			for (int i = 0; i < names.Length; i++)
			{
				var name = names[i];
				var property = type.GetProperty(name, flags);
				if (property != null) return property;
				var field = type.GetField(name, flags);
				if (field != null) return field;
			}
			return null;
		}

		private static Type? GetMemberType(MemberInfo member)
		{
			return member switch
			{
				PropertyInfo p => p.PropertyType,
				FieldInfo f => f.FieldType,
				_ => null
			};
		}

		private static object? GetMemberValue(MemberInfo member, object target)
		{
			return member switch
			{
				PropertyInfo p => p.GetValue(target),
				FieldInfo f => f.GetValue(target),
				_ => null
			};
		}

		private static IEnumerable<object> EnumerateObjects(object? source)
		{
			if (source == null || source is string) yield break;

			if (source is IEnumerable enumerable)
			{
				foreach (var entry in enumerable)
				{
					yield return entry!;
				}
				yield break;
			}

			var sourceType = source.GetType();
			var backingMember = FindMember(sourceType, DListBackingNames);
			if (backingMember != null)
			{
				var backingValue = GetMemberValue(backingMember, source);
				if (backingValue != null)
				{
					foreach (var entry in EnumerateObjects(backingValue))
					{
						yield return entry;
					}
					yield break;
				}
			}

			// Fallback for IL2CPP containers that expose Count/Length + indexer but not IEnumerable.
			var countMember = FindMember(sourceType, new[] { "Count", "count", "Length", "length" });
			var itemProperty = sourceType.GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (countMember != null && itemProperty != null)
			{
				var countObj = GetMemberValue(countMember, source);
				if (countObj is int count && count >= 0)
				{
					for (int i = 0; i < count; i++)
					{
						object? value = null;
						try
						{
							value = itemProperty.GetValue(source, new object[] { i });
						}
						catch
						{
							// Ignore broken element access; continue scanning.
						}

						if (value != null) yield return value;
					}
				}
			}
		}

		private static Type? ResolveCollectionElementType(Type collectionType)
		{
			if (collectionType.IsArray)
			{
				return collectionType.GetElementType();
			}

			if (collectionType.IsGenericType)
			{
				var args = collectionType.GetGenericArguments();
				if (args.Length == 1)
				{
					return args[0];
				}
			}

			return null;
		}

		private static void LogReflectionFailureOnce(string reason)
		{
			if (s_loggedReflectionFailure) return;
			s_loggedReflectionFailure = true;
			MelonLogger.Error("[LEHud.ESP.Barrels] " + reason);
		}

		private static void LogAlignmentFallbackOnce(string reason)
		{
			if (s_loggedAlignmentFallback) return;
			s_loggedAlignmentFallback = true;
			MelonLogger.Msg("[LEHud.ESP.Barrels] " + reason);
		}

	}
}
