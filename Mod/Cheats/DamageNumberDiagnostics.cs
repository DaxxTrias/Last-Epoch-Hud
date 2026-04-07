using System.Reflection;
using System.Text;
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
			public string Style = string.Empty;
			public string Color = string.Empty;
			public Vector3 Position;
			public float LastSeenAt;
			public int InitCount;
			public int SendCount;
		}

		private static readonly Dictionary<int, DamageNumberState> s_states = new Dictionary<int, DamageNumberState>(512);
		private static readonly StringBuilder s_summaryBuilder = new StringBuilder(512);

		private static Type? s_cachedDamageNumberType;
		private static FieldInfo? s_tmpField;
		private static PropertyInfo? s_tmpTextProperty;

		private static float s_periodStartAt = -1f;
		private static int s_periodAwakeCount;
		private static int s_periodInitCount;
		private static int s_periodSendCount;
		private static int s_periodDestroyCount;

		private static readonly TimeSpan SendLogInterval = TimeSpan.FromSeconds(2);

		public static void OnPrefix(object __instance, object[]? __args, MethodBase? __originalMethod)
		{
			if (!Settings.enableDamageNumberDiagnostics || __originalMethod == null)
				return;

			if (!string.Equals(__originalMethod.Name, "OnDestroy", StringComparison.Ordinal))
				return;

			Observe(__instance, __args, __originalMethod, phase: "Prefix");
		}

		public static void OnPostfix(object __instance, object[]? __args, MethodBase? __originalMethod)
		{
			if (!Settings.enableDamageNumberDiagnostics || __originalMethod == null)
				return;

			if (string.Equals(__originalMethod.Name, "OnDestroy", StringComparison.Ordinal))
				return;

			Observe(__instance, __args, __originalMethod, phase: "Postfix");
		}

		private static void Observe(object __instance, object[]? __args, MethodBase __originalMethod, string phase)
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
					Log.InfoThrottled(LogSource.Hooks, "DamageNumber.Awake", "[DamageNumberDiag] Awake observed.", TimeSpan.FromSeconds(3));
					break;
				case "Init":
					s_periodInitCount++;
					if (state != null)
					{
						state.InitCount++;
						state.LastSeenAt = now;
						CaptureInitData(state, __args, __originalMethod);
					}
					LogInitSample(id, state, __args, __instance, __originalMethod);
					break;
				case "SendPropertiesToRenderer":
					s_periodSendCount++;
					if (state != null)
					{
						state.SendCount++;
						state.LastSeenAt = now;
						CaptureRendererData(state, __instance);
					}
					Log.InfoThrottled(
						LogSource.Hooks,
						"DamageNumber.SendPropertiesToRenderer",
						$"[DamageNumberDiag] SendPropertiesToRenderer observed (offline={ObjectManager.IsOfflineMode()}).",
						SendLogInterval);
					break;
				case "OnDestroy":
					s_periodDestroyCount++;
					if (id != 0)
						s_states.Remove(id);
					Log.InfoThrottled(LogSource.Hooks, "DamageNumber.OnDestroy", "[DamageNumberDiag] OnDestroy observed.", TimeSpan.FromSeconds(3));
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

		private static void CaptureInitData(DamageNumberState state, object[]? args, MethodBase method)
		{
			if (args == null || args.Length == 0)
				return;

			var parameters = method.GetParameters();
			int max = Math.Min(parameters.Length, args.Length);
			for (int i = 0; i < max; i++)
			{
				var arg = args[i];
				if (arg == null)
					continue;

				string paramName = parameters[i].Name ?? string.Empty;
				Type paramType = parameters[i].ParameterType;

				if (arg is Vector3 position)
				{
					state.Position = position;
					continue;
				}

				if (arg is Color color)
				{
					state.Color = $"{color.r:F2},{color.g:F2},{color.b:F2},{color.a:F2}";
					continue;
				}

				if (arg is string text && !string.IsNullOrWhiteSpace(text))
				{
					state.Text = Sanitize(text);
					continue;
				}

				if (paramType.IsEnum || paramName.IndexOf("style", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					state.Style = Sanitize(arg.ToString() ?? string.Empty);
				}
			}
		}

		private static void CaptureRendererData(DamageNumberState state, object instance)
		{
			try
			{
				var instanceType = instance.GetType();
				EnsureBindings(instanceType);
				if (s_tmpField == null)
					return;

				var tmp = s_tmpField.GetValue(instance);
				if (tmp == null || s_tmpTextProperty == null)
					return;

				var textObj = s_tmpTextProperty.GetValue(tmp);
				if (textObj == null)
					return;

				string text = Sanitize(textObj.ToString() ?? string.Empty);
				if (!string.IsNullOrWhiteSpace(text))
				{
					state.Text = text;
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
			s_tmpField = AccessTools.Field(instanceType, "tmp");
			s_tmpTextProperty = null;
			if (s_tmpField != null)
			{
				var tmpType = s_tmpField.FieldType;
				s_tmpTextProperty = AccessTools.Property(tmpType, "text");
			}
		}

		private static int TryGetInstanceId(object? instance)
		{
			if (instance is UnityEngine.Object unityObj)
			{
				return unityObj.GetInstanceID();
			}

			return 0;
		}

		private static void LogInitSample(int instanceId, DamageNumberState? state, object[]? args, object instance, MethodBase method)
		{
			s_summaryBuilder.Clear();
			s_summaryBuilder.Append("[DamageNumberDiag] Init ")
				.Append("id=").Append(instanceId)
				.Append(" offline=").Append(ObjectManager.IsOfflineMode());

			if (state != null)
			{
				if (!string.IsNullOrWhiteSpace(state.Text))
					s_summaryBuilder.Append(" text='").Append(state.Text).Append('\'');
				if (!string.IsNullOrWhiteSpace(state.Style))
					s_summaryBuilder.Append(" style=").Append(state.Style);
				if (!string.IsNullOrWhiteSpace(state.Color))
					s_summaryBuilder.Append(" color=").Append(state.Color);
				s_summaryBuilder.Append(" pos=").Append(FormatVec3(state.Position));
			}

			AppendInitArgs(s_summaryBuilder, method, args);
			Log.Info(LogSource.Hooks, s_summaryBuilder.ToString());
		}

		private static void AppendInitArgs(StringBuilder sb, MethodBase method, object[]? args)
		{
			if (args == null || args.Length == 0)
				return;

			var parameters = method.GetParameters();
			int max = Math.Min(parameters.Length, args.Length);
			for (int i = 0; i < max; i++)
			{
				string name = parameters[i].Name ?? $"arg{i}";
				string value = Sanitize(args[i]?.ToString() ?? "null");
				sb.Append(" | ").Append(name).Append('=').Append(value);
			}
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

		private static string FormatVec3(Vector3 vec)
		{
			return $"{vec.x:F1},{vec.y:F1},{vec.z:F1}";
		}

		private static void MaybeEmitPeriodSummary(float now, string phase, string lastMethod)
		{
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
	}
}
