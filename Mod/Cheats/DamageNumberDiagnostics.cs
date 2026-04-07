using System.Reflection;
using System.Text;
using System.Linq;
using HarmonyLib;
using Mod.Game;
using Mod.Utils;
using UnityEngine;

namespace Mod.Cheats
{
	internal static class DamageNumberDiagnostics
	{
		private sealed class DamageNumberState
		{
			public string Text = string.Empty;
			public Color LastColor = Color.white;
			public bool HasLastColor;
			public float LastSeenAt;
			public float LastDeepTextProbeAt;
			public int SendCount;
		}

		private static readonly Dictionary<int, DamageNumberState> s_states = new Dictionary<int, DamageNumberState>(512);
		private static readonly StringBuilder s_summaryBuilder = new StringBuilder(512);

		private static Type? s_cachedDamageNumberType;
		private static FieldInfo? s_tmpField;
		private static PropertyInfo? s_tmpProperty;
		private static PropertyInfo? s_tmpTextProperty;
		private static PropertyInfo? s_tmpColorProperty;
		private static bool s_loggedTypeShape;
		private static bool s_loggedTextBinding;

		private static float s_periodStartAt = -1f;
		private static int s_periodAwakeCount;
		private static int s_periodInitCount;
		private static int s_periodSendCount;
		private static int s_periodDestroyCount;

		private static readonly TimeSpan SendLogInterval = TimeSpan.FromSeconds(2);

		public static void OnPrefix(object __instance, MethodBase? __originalMethod)
		{
			if (!IsActive() || __originalMethod == null)
				return;

			if (!string.Equals(__originalMethod.Name, "OnDestroy", StringComparison.Ordinal))
				return;

			Observe(__instance, __originalMethod, phase: "Prefix");
		}

		public static void OnPostfix(object __instance, MethodBase? __originalMethod)
		{
			if (!IsActive() || __originalMethod == null)
				return;

			if (string.Equals(__originalMethod.Name, "OnDestroy", StringComparison.Ordinal))
				return;

			Observe(__instance, __originalMethod, phase: "Postfix");
		}

		public static void OnInitPostfix(object __instance, MethodBase? __originalMethod)
		{
			if (!Settings.enableDamageNumberDiagnostics || __originalMethod == null)
				return;

			try
			{
				float now = Time.unscaledTime;
				if (s_periodStartAt <= 0f)
					s_periodStartAt = now;

				int id = TryGetInstanceId(__instance);
				DamageNumberState? state = null;
				if (id != 0)
				{
					state = GetOrCreateState(id);
					state.LastSeenAt = now;
					CaptureRendererData(state, __instance);
				}

				s_periodInitCount++;
				string overloadKey = BuildInitOverloadKey(__originalMethod);
				Log.InfoThrottled(
					LogSource.Hooks,
					$"DamageNumber.Init.{overloadKey}",
					$"[DamageNumberDiag] Init observed ({overloadKey}) (offline={ObjectManager.IsOfflineMode()}, id={id}, text='{SafeStateText(state)}').",
					TimeSpan.FromSeconds(2));

				MaybeEmitPeriodSummary(now, "Postfix", "Init");
			}
			catch
			{
				// diagnostics best effort
			}
		}

		private static void Observe(object __instance, MethodBase __originalMethod, string phase)
		{
			float now = Time.unscaledTime;
			if (s_periodStartAt <= 0f)
				s_periodStartAt = now;

			int id = TryGetInstanceId(__instance);
			DamageNumberState? state = null;
			if (id != 0)
				state = GetOrCreateState(id);

			string methodName = __originalMethod.Name;
			switch (methodName)
			{
				case "Awake":
					s_periodAwakeCount++;
					if (state != null)
						state.LastSeenAt = now;
					if (Settings.enableDamageNumberDiagnostics)
					{
						Log.InfoThrottled(LogSource.Hooks, "DamageNumber.Awake", "[DamageNumberDiag] Awake observed.", TimeSpan.FromSeconds(3));
					}
					break;
				case "SendPropertiesToRenderer":
					s_periodSendCount++;
					if (state != null)
					{
						state.SendCount++;
						state.LastSeenAt = now;
						CaptureRendererData(state, __instance);
					}
					if (state != null && !string.IsNullOrWhiteSpace(state.Text))
					{
						DpsMeter.OnOnlineDamageTextSample(__instance, state.Text, state.HasLastColor ? state.LastColor : null);
					}
					if (Settings.enableDamageNumberDiagnostics)
					{
						Log.InfoThrottled(
							LogSource.Hooks,
							"DamageNumber.SendPropertiesToRenderer",
							$"[DamageNumberDiag] SendPropertiesToRenderer observed (offline={ObjectManager.IsOfflineMode()}, id={id}, text='{SafeStateText(state)}', color={SafeStateColor(state)}).",
							SendLogInterval);
					}
					break;
				case "OnDestroy":
					s_periodDestroyCount++;
					if (id != 0)
						s_states.Remove(id);
					if (Settings.enableDamageNumberDiagnostics)
					{
						Log.InfoThrottled(LogSource.Hooks, "DamageNumber.OnDestroy", "[DamageNumberDiag] OnDestroy observed.", TimeSpan.FromSeconds(3));
					}
					break;
			}

			MaybeEmitPeriodSummary(now, phase, methodName);
		}

		private static DamageNumberState GetOrCreateState(int instanceId)
		{
			if (!s_states.TryGetValue(instanceId, out var state))
			{
				state = new DamageNumberState();
				s_states[instanceId] = state;
			}

			return state;
		}

		private static void CaptureRendererData(DamageNumberState state, object instance)
		{
			try
			{
				var instanceType = instance.GetType();
				EnsureBindings(instanceType);

				if (TryReadTextFromBoundField(instance, out string? boundText))
				{
					state.Text = boundText!;
				}
				if (TryReadColorFromBoundField(instance, out var boundColor))
				{
					state.LastColor = boundColor;
					state.HasLastColor = true;
				}
				if (!string.IsNullOrWhiteSpace(state.Text))
					return;

				// Slow fallback probe (rate-limited): inspect child components for text-bearing fields.
				float now = Time.unscaledTime;
				if (now - state.LastDeepTextProbeAt < 1.5f)
					return;
				state.LastDeepTextProbeAt = now;

				if (instance is Component c && c.gameObject != null && TryReadTextFromHierarchy(c.gameObject, out string? hierarchyText))
				{
					state.Text = hierarchyText!;
				}
			}
			catch
			{
				// Diagnostics-only path.
			}
		}

		private static void EnsureBindings(Type instanceType)
		{
			if (s_cachedDamageNumberType == instanceType)
				return;

			s_cachedDamageNumberType = instanceType;
			s_tmpField = null;
			s_tmpProperty = null;
			s_tmpTextProperty = null;
			s_tmpColorProperty = null;

			// Avoid AccessTools.Field warnings by resolving manually.
			s_tmpField = FindFieldRecursive(instanceType, "tmp")
				?? FindFieldRecursive(instanceType, "_tmp")
				?? FindFieldRecursive(instanceType, "textMesh")
				?? FindFieldRecursive(instanceType, "textMeshPro")
				?? FindFirstTextLikeField(instanceType);

			s_tmpProperty = FindPropertyRecursive(instanceType, "tmp")
				?? FindPropertyRecursive(instanceType, "_tmp")
				?? FindPropertyRecursive(instanceType, "textMesh")
				?? FindPropertyRecursive(instanceType, "textMeshPro")
				?? FindFirstTextLikeProperty(instanceType);

			Type? tmpContainerType = s_tmpField?.FieldType ?? s_tmpProperty?.PropertyType;
			if (tmpContainerType != null && TryResolveTextProperty(tmpContainerType, out var textProp))
			{
				s_tmpTextProperty = textProp;
			}
			if (tmpContainerType != null && TryResolveColorProperty(tmpContainerType, out var colorProp))
			{
				s_tmpColorProperty = colorProp;
			}

			LogTypeShape(instanceType);
			LogResolvedBindings(instanceType);
		}

		private static FieldInfo? FindFieldRecursive(Type type, string name)
		{
			for (var t = type; t != null; t = t.BaseType)
			{
				var field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				if (field != null)
					return field;
			}

			return null;
		}

		private static FieldInfo? FindFirstTextLikeField(Type type)
		{
			for (var t = type; t != null; t = t.BaseType)
			{
				var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				for (int i = 0; i < fields.Length; i++)
				{
					var f = fields[i];
					string typeName = f.FieldType.FullName ?? f.FieldType.Name;
					if (typeName.IndexOf("TMPro", StringComparison.OrdinalIgnoreCase) >= 0
						|| typeName.IndexOf("TextMesh", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return f;
					}
				}
			}

			return null;
		}

		private static PropertyInfo? FindPropertyRecursive(Type type, string name)
		{
			for (var t = type; t != null; t = t.BaseType)
			{
				var property = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				if (property == null || property.GetIndexParameters().Length != 0)
					continue;

				return property;
			}

			return null;
		}

		private static PropertyInfo? FindFirstTextLikeProperty(Type type)
		{
			for (var t = type; t != null; t = t.BaseType)
			{
				var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				for (int i = 0; i < properties.Length; i++)
				{
					var p = properties[i];
					if (p.GetIndexParameters().Length != 0)
						continue;

					string typeName = p.PropertyType.FullName ?? p.PropertyType.Name;
					if (typeName.IndexOf("TMPro", StringComparison.OrdinalIgnoreCase) >= 0
						|| typeName.IndexOf("TextMesh", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return p;
					}
				}
			}

			return null;
		}

		private static bool TryResolveTextProperty(Type targetType, out PropertyInfo? textProperty)
		{
			textProperty = null;
			try
			{
				textProperty = targetType.GetProperty("text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (textProperty != null)
					return true;

				textProperty = targetType.GetProperty("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				return textProperty != null;
			}
			catch
			{
				textProperty = null;
				return false;
			}
		}

		private static bool TryResolveColorProperty(Type targetType, out PropertyInfo? colorProperty)
		{
			colorProperty = null;
			try
			{
				colorProperty = targetType.GetProperty("color", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (colorProperty != null)
					return true;

				colorProperty = targetType.GetProperty("Color", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				return colorProperty != null;
			}
			catch
			{
				colorProperty = null;
				return false;
			}
		}

		private static bool TryReadTextFromBoundField(object instance, out string? text)
		{
			text = null;
			if ((s_tmpField == null && s_tmpProperty == null) || s_tmpTextProperty == null)
				return false;

			try
			{
				object? tmp = null;
				if (s_tmpField != null)
				{
					tmp = s_tmpField.GetValue(instance);
				}
				else if (s_tmpProperty != null)
				{
					tmp = s_tmpProperty.GetValue(instance);
				}

				if (tmp == null)
					return false;

				var value = s_tmpTextProperty.GetValue(tmp);
				if (value == null)
					return false;

				string parsed = Sanitize(value.ToString() ?? string.Empty);
				if (string.IsNullOrWhiteSpace(parsed))
					return false;

				text = parsed;
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static bool TryReadColorFromBoundField(object instance, out Color color)
		{
			color = default;
			if ((s_tmpField == null && s_tmpProperty == null) || s_tmpColorProperty == null)
				return false;

			try
			{
				object? tmp = null;
				if (s_tmpField != null)
				{
					tmp = s_tmpField.GetValue(instance);
				}
				else if (s_tmpProperty != null)
				{
					tmp = s_tmpProperty.GetValue(instance);
				}

				if (tmp == null)
					return false;

				var value = s_tmpColorProperty.GetValue(tmp);
				if (value == null)
					return false;

				if (value is Color c)
				{
					color = c;
					return true;
				}

				if (value is Color32 c32)
				{
					color = c32;
					return true;
				}
			}
			catch
			{
				// diagnostics best effort
			}

			return false;
		}

		private static bool TryReadTextFromHierarchy(GameObject root, out string? text)
		{
			text = null;
			try
			{
				var components = root.GetComponentsInChildren<Component>(true);
				for (int i = 0; i < components.Length; i++)
				{
					var comp = components[i];
					if (comp == null)
						continue;

					var compType = comp.GetType();
					string compTypeName = compType.FullName ?? compType.Name;
					if (compTypeName.IndexOf("TMPro", StringComparison.OrdinalIgnoreCase) < 0
						&& compTypeName.IndexOf("TextMesh", StringComparison.OrdinalIgnoreCase) < 0
						&& compTypeName.IndexOf("TMP", StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}

					if (!TryResolveTextProperty(compType, out var textProp) || textProp == null)
						continue;

					var value = textProp.GetValue(comp);
					if (value == null)
						continue;

					string parsed = Sanitize(value.ToString() ?? string.Empty);
					if (string.IsNullOrWhiteSpace(parsed))
						continue;

					text = parsed;
					Log.InfoThrottled(
						LogSource.Hooks,
						$"DamageNumberDiag.TextHierarchy.{compTypeName}",
						$"[DamageNumberDiag] Hierarchy text binding resolved via {compTypeName}.text",
						TimeSpan.FromSeconds(30));
					return true;
				}
			}
			catch
			{
				// diagnostics best effort
			}

			return false;
		}

		private static void LogTypeShape(Type instanceType)
		{
			if (!Settings.enableDamageNumberDiagnostics)
				return;
			if (s_loggedTypeShape)
				return;
			s_loggedTypeShape = true;

			try
			{
				var fields = instanceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				var props = instanceType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				var methods = instanceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(m => string.Equals(m.Name, "Init", StringComparison.Ordinal))
					.ToArray();

				var sb = new StringBuilder(900);
				sb.Append("[DamageNumberDiag] Type shape: ").Append(instanceType.FullName ?? instanceType.Name);
				sb.Append(" | fields=");
				int appended = 0;
				for (int i = 0; i < fields.Length; i++)
				{
					var f = fields[i];
					string ft = f.FieldType.FullName ?? f.FieldType.Name;
					bool interesting = f.Name.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0
						|| f.Name.IndexOf("tmp", StringComparison.OrdinalIgnoreCase) >= 0
						|| f.Name.IndexOf("render", StringComparison.OrdinalIgnoreCase) >= 0
						|| ft.IndexOf("TMPro", StringComparison.OrdinalIgnoreCase) >= 0
						|| ft.IndexOf("TextMesh", StringComparison.OrdinalIgnoreCase) >= 0
						|| ft.IndexOf("Renderer", StringComparison.OrdinalIgnoreCase) >= 0;
					if (!interesting)
						continue;

					if (appended++ > 0)
						sb.Append(", ");
					sb.Append(f.Name).Append(':').Append(ft);
				}

				sb.Append(" | props=");
				appended = 0;
				for (int i = 0; i < props.Length; i++)
				{
					var p = props[i];
					string pt = p.PropertyType.FullName ?? p.PropertyType.Name;
					bool interesting = p.Name.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0
						|| p.Name.IndexOf("tmp", StringComparison.OrdinalIgnoreCase) >= 0
						|| pt.IndexOf("TMPro", StringComparison.OrdinalIgnoreCase) >= 0
						|| pt.IndexOf("TextMesh", StringComparison.OrdinalIgnoreCase) >= 0;
					if (!interesting)
						continue;

					if (appended++ > 0)
						sb.Append(", ");
					sb.Append(p.Name).Append(':').Append(pt);
				}

				for (int i = 0; i < methods.Length; i++)
				{
					var m = methods[i];
					var ps = m.GetParameters();
					sb.Append(" | Init(");
					for (int p = 0; p < ps.Length; p++)
					{
						if (p > 0) sb.Append(", ");
						sb.Append(ps[p].ParameterType.Name).Append(' ').Append(ps[p].Name);
					}
					sb.Append(')');
				}

				Log.Info(LogSource.Hooks, sb.ToString());
			}
			catch (Exception ex)
			{
				Log.Warning(LogSource.Hooks, $"[DamageNumberDiag] Type shape probe failed: {ex.GetType().Name} {ex.Message}");
			}
		}

		private static void LogResolvedBindings(Type instanceType)
		{
			if (!Settings.enableDamageNumberDiagnostics)
				return;
			if (s_loggedTextBinding)
				return;
			s_loggedTextBinding = true;

			string fieldName = s_tmpField?.Name ?? "<none>";
			string fieldType = s_tmpField?.FieldType.FullName ?? "<none>";
			string tmpPropName = s_tmpProperty?.Name ?? "<none>";
			string tmpPropType = s_tmpProperty?.PropertyType.FullName ?? "<none>";
			string textPropName = s_tmpTextProperty?.Name ?? "<none>";
			string colorPropName = s_tmpColorProperty?.Name ?? "<none>";
			Log.Info(LogSource.Hooks, $"[DamageNumberDiag] Text binding: field={fieldName}:{fieldType}, tmpProp={tmpPropName}:{tmpPropType}, textProp={textPropName}, colorProp={colorPropName} on {instanceType.FullName ?? instanceType.Name}");
		}

		private static int TryGetInstanceId(object? instance)
		{
			if (instance is UnityEngine.Object unityObj)
			{
				return unityObj.GetInstanceID();
			}

			return 0;
		}

		private static string Sanitize(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return value;

			var chars = value.ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				if (char.IsControl(chars[i]))
					chars[i] = ' ';
			}

			return new string(chars).Trim();
		}

		private static void MaybeEmitPeriodSummary(float now, string phase, string lastMethod)
		{
			if (!Settings.enableDamageNumberDiagnostics)
				return;
			if (now - s_periodStartAt < 5f)
				return;

			s_summaryBuilder.Clear();
			s_summaryBuilder.Append("[DamageNumberDiag] 5s summary")
				.Append(" offline=").Append(ObjectManager.IsOfflineMode())
				.Append(" awake=").Append(s_periodAwakeCount)
				.Append(" init=").Append(s_periodInitCount)
				.Append(" send=").Append(s_periodSendCount)
				.Append(" destroy=").Append(s_periodDestroyCount)
				.Append(" tracked=").Append(s_states.Count)
				.Append(" last=").Append(lastMethod)
				.Append('.').Append(phase);

			Log.Info(LogSource.Hooks, s_summaryBuilder.ToString());

			s_periodStartAt = now;
			s_periodAwakeCount = 0;
			s_periodInitCount = 0;
			s_periodSendCount = 0;
			s_periodDestroyCount = 0;
		}

		private static string SafeStateText(DamageNumberState? state)
		{
			if (state == null || string.IsNullOrWhiteSpace(state.Text))
				return "-";
			return state.Text;
		}

		private static string SafeStateColor(DamageNumberState? state)
		{
			if (state == null || !state.HasLastColor)
				return "-";
			return $"{state.LastColor.r:F2},{state.LastColor.g:F2},{state.LastColor.b:F2},{state.LastColor.a:F2}";
		}

		private static bool IsActive()
		{
			return Settings.enableDamageNumberDiagnostics
				|| (Settings.enableDpsMeter && Settings.enableDpsMeterOnlineRaw);
		}

		private static string BuildInitOverloadKey(MethodBase method)
		{
			try
			{
				var ps = method.GetParameters();
				if (ps.Length == 0)
					return "none";

				var sb = new StringBuilder(96);
				for (int i = 0; i < ps.Length; i++)
				{
					if (i > 0)
						sb.Append(',');
					sb.Append(ps[i].ParameterType.Name);
				}

				return sb.ToString();
			}
			catch
			{
				return "unknown";
			}
		}
	}
}
