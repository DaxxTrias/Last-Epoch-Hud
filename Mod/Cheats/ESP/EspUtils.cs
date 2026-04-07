using System;
using UnityEngine;

namespace Mod.Cheats.ESP
{
	internal static class EspUtils
	{
		public static string SanitizeLabel(string? value)
		{
			if (string.IsNullOrEmpty(value)) return string.Empty;
			var sanitized = value.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
			return sanitized.Trim();
		}

		public static bool IsComponentEnabled(Component comp)
		{
			try
			{
				var behaviour = comp as Behaviour;
				if (behaviour != null) return behaviour.enabled;
			}
			catch (Exception) { }
			return true;
		}
	}
}
