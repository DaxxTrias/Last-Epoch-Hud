using UnityEngine;
using Color = UnityEngine.Color;
using MelonLoader;

namespace Mod
{
	internal class Drawing
	{
		public static Texture2D? lineTex = new Texture2D(1, 1);
		public static GUIStyle? StringStyle { get; private set; }

		// private static int debugLastFrame = -1;
		// private static int debugLogsThisFrame = 0;
		// private const int DebugMaxLogsPerFrame = 8;

		public static readonly Color BloodOrange = new Color(1.00f, 0.50f, 0.00f, 1f);

		static Vector2 ClampToScreen(Vector3 vecIn, Vector3 padding)
		{
			if (vecIn.z < 0)
			{
				vecIn *= -1;
			}

			return new Vector2(
							  Mathf.Clamp(vecIn.x, padding.x, Screen.width - padding.x),
							  Mathf.Clamp(vecIn.y, padding.y, Screen.height - padding.y)
							 );
		}

		public static void SetupGuiStyle()
		{
			// Must be called from within OnGUI
			if (StringStyle == null)
			{
				StringStyle = new GUIStyle(GUI.skin.label);
				StringStyle.clipping = TextClipping.Overflow;
				StringStyle.wordWrap = false;
				// Ensure default text color is neutral; specific draws will override
				StringStyle.normal.textColor = Color.white;
			}
		}

		public static void DrawString(Vector3 worldPosition, string label, bool centered = true)
		{
			// Respect label visibility toggle
			if (!Settings.showESPLabels)
				return;

			var cam = Camera.main;
			if (cam == null) return;
			Vector3 screen = cam.WorldToScreenPoint(worldPosition);
			screen.y = Screen.height - screen.y;
			// Clamp the label to the screen
			Vector2 position = ClampToScreen(screen, new Vector2(25, 25));

			var content = new GUIContent(label);
			var style = StringStyle ?? new GUIStyle();
			var size = style.CalcSize(content);
			var upperLeft = centered ? position - size / 2f : position;

			// if (Settings.debugESPNames)
			// {
			// 	if (debugLastFrame != Time.frameCount)
			// 	{
			// 		debugLastFrame = Time.frameCount;
			// 		debugLogsThisFrame = 0;
			// 	}
			// 	if (debugLogsThisFrame < DebugMaxLogsPerFrame)
			// 	{
			// 		debugLogsThisFrame++;
			// 		string printable = label.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
			// 		MelonLogger.Msg($"ESP Name Debug: '{printable}' len={label.Length} size=({size.x:F1},{size.y:F1}) screen=({screen.x:F1},{screen.y:F1}) clamped=({position.x:F1},{position.y:F1}) rect=({upperLeft.x:F1},{upperLeft.y:F1},{size.x:F1},{size.y:F1})");
			// 	}
			// }

			GUI.Label(new Rect(upperLeft, size), content, style);
		}

		public static void DrawString(Vector3 worldPosition, string label, Color color, bool centered = true)
		{
			// Respect label visibility toggle
			if (!Settings.showESPLabels)
				return;

			var style = StringStyle ?? new GUIStyle();
			// Backup colors to avoid leaking state across GUI calls
			var backupTextColor = style.normal.textColor;
			var prevContentColor = GUI.contentColor;
			var prevGuiColor = GUI.color;

			// Neutralize any global GUI tint; rely solely on style text color
			GUI.contentColor = Color.white;
			GUI.color = Color.white;

			// Apply the requested color for this draw only via style
			style.normal.textColor = color;

			DrawString(worldPosition, label, centered);

			// Restore previous colors
			style.normal.textColor = backupTextColor;
			GUI.contentColor = prevContentColor;
			GUI.color = prevGuiColor;
		}

		public static void DrawCustomString(Vector3 worldPosition, string label, Color color, bool centered = true)
		{
			// Respect label visibility toggle
			if (!Settings.showESPLabels)
				return;

			var cam = Camera.main;
			if (cam == null) return;
			Vector3 screen = cam.WorldToScreenPoint(worldPosition);
			screen.y = Screen.height - screen.y;

			// Clamp the label to the screen
			Vector2 position = ClampToScreen(screen, new Vector2(25, 25));

			// Split the label into two parts: first 3 characters and the rest
			string firstPart = label.Length > 3 ? label.Substring(0, 3) : label;
			string secondPart = label.Length > 3 ? label.Substring(3) : string.Empty;

			var firstContent = new GUIContent(firstPart);
			var secondContent = new GUIContent(secondPart);

			var style = StringStyle ?? new GUIStyle();
			var firstSize = style.CalcSize(firstContent);
			var secondSize = style.CalcSize(secondContent);

			var totalSize = new Vector2(firstSize.x + secondSize.x, Mathf.Max(firstSize.y, secondSize.y));
			var upperLeft = centered ? position - totalSize / 2f : position;

			// Backup the current GUI color
			Color prevColor = GUI.color;

			// Draw the first part with the custom color
			GUI.color = color;
			GUI.Label(new Rect(upperLeft, firstSize), firstContent, style);

			// Draw the second part with the default color
			GUI.color = prevColor;
			GUI.Label(new Rect(new Vector2(upperLeft.x + firstSize.x, upperLeft.y), secondSize), secondContent, style);

			// Restore the previous GUI color
			GUI.color = prevColor;
		}

		public static void DrawLine(Vector3 worldA, Vector3 worldB, Color color, float width)
		{
			// Respect line visibility toggle
			if (!Settings.showESPLines)
				return;

			if (lineTex == null)
			{
				lineTex = new Texture2D(1, 1);
				lineTex.SetPixel(0, 0, Color.white);
				lineTex.Apply();
			}

			Color prevColor = GUI.color;
			Matrix4x4 prevMatrix = GUI.matrix;

			var cam = Camera.main;
			if (cam == null) return;
			Vector3 screenA = cam.WorldToScreenPoint(worldA);
			Vector3 screenB = cam.WorldToScreenPoint(worldB);

			screenA.y = Screen.height - screenA.y;
			screenB.y = Screen.height - screenB.y;


			// Clamp points to screen with padding
			Vector2 pointA = ClampToScreen(screenA, new Vector2(25, 25));
			Vector2 pointB = ClampToScreen(screenB, new Vector2(25, 25));

			// Calculate angle and magnitude
			float angle = Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x) * 180f / Mathf.PI;
			float magnitude = (pointB - pointA).magnitude;

			// Apply color
			GUI.color = color;

			// Create matrix for rotation
			Matrix4x4 matrix = Matrix4x4.TRS(pointA, Quaternion.Euler(0, 0, angle), Vector3.one);

			// Apply the matrix
			GUI.matrix = matrix;

			// Draw the line
			GUI.DrawTexture(new Rect(0, -width / 2, magnitude, width), lineTex);

			// Revert GUI color and matrix to previous state
			GUI.color = prevColor;
			GUI.matrix = prevMatrix;
		}

		public static Color ItemRarityToColor(string rarity)
		{
			var color = Color.white;

			if (rarity.Contains("Magic"))
			{
				color = Color.blue;
			}
			else if (rarity.Contains("Common"))
			{
				color = Color.white;
			}
			else if (rarity.Contains("Unique"))
			{
				color = Color.red;
			}
			else if (rarity.Contains("Legendary"))
			{
				color = new Color(1.0f, 0.5f, 0.0f);
			}
			else if (rarity.Contains("Rare"))
			{
				color = Color.yellow;
			}
			else if (rarity.Contains("Set"))
			{
				color = Color.green;
			}
			else if (rarity.Contains("Exalted"))
			{
				color = new Color(0.5f, 0, 0.5f);
			}

			return color;
		}

		public static Color AlignmentToColor(string alignment)
		{
			var color = Color.white;
			switch (alignment)
			{
				case "Good":
					color = Color.green;
					break;
				case "Evil":
					color = Color.red;
					break;
				case "Barrel":
					color = Color.yellow;
					break;
				case "HostileNeutral":
					color = Color.blue;
					break;
				case "FriendlyNeutral":
					color = Color.cyan;
					break;
				case "SummonedCorpse":
					color = Color.magenta;
					break;
			}

			return color;
		}

		public static void Initialize()
		{
			if (lineTex != null)
			{
				lineTex.SetPixel(0, 0, Color.white);
				lineTex.Apply();
			}
		}

		public static void Cleanup()
		{
			if (lineTex != null)
			{
				UnityEngine.Object.Destroy(lineTex);
				lineTex = null;
			}
			// Do not touch GUI.skin here; class can now be safely touched outside OnGUI
		}
	}
}
