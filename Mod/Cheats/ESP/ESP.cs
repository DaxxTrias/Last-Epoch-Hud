using Mod.Game;
using UnityEngine;

namespace Mod.Cheats.ESP
{
    internal enum EspStringStyle
    {
        Default = 0,
        Emphasized = 1
    }

    internal readonly struct LineDrawing
    {
        private readonly Vector3 start;
        private readonly Vector3 end;
        private readonly Color color;

        public LineDrawing(Vector3 start, Vector3 end, Color color)
        {
            this.start = start;
            this.end = end;
            this.color = color;
        }

        public void Draw()
        {
            Drawing.DrawLine(start, end, color, 2);
        }
    }

    internal readonly struct StringDrawing
    {
        private readonly string text;
        private readonly Vector3 position;
        private readonly Color color;
        private readonly EspStringStyle style;

        public StringDrawing(string text, Vector3 position, Color color, EspStringStyle style)
        {
            this.text = text;
            this.position = position;
            this.color = color;
            this.style = style;
        }

        public void Draw()
        {
            if (style == EspStringStyle.Emphasized)
            {
                Drawing.DrawStringEmphasized(position, text, color);
                return;
            }

            Drawing.DrawString(position, text, color);
        }
    }

    internal class ESP
    {
        public static readonly List<LineDrawing> lineDrawings = new List<LineDrawing>();
        public static readonly List<StringDrawing> stringDrawings = new List<StringDrawing>();

        public static void AddLine(Vector3 start, Vector3 end, Color color)
        {
            lineDrawings.Add(new LineDrawing(start, end, color));
        }

        public static void AddString(string text, Vector3 position, Color color, EspStringStyle style = EspStringStyle.Default)
        {
            stringDrawings.Add(new StringDrawing(text, position, color, style));
        }

        public static void Draw()
        {
            for (int i = 0; i < lineDrawings.Count; i++)
            {
                lineDrawings[i].Draw();
            }

            for (int i = 0; i < stringDrawings.Count; i++)
            {
                stringDrawings[i].Draw();
            }
        }

        public static void Clear()
        {
            lineDrawings.Clear();
            stringDrawings.Clear();
        }

        public static void OnGUI()
        {
            Draw();
        }

        public static void OnUpdate()
		{
			Clear();
			var player = ObjectManager.GetLocalPlayer();
			if (player == null) return;

			Items.OnUpdate(player);
			GoldPiles.OnUpdate(player);
			Shrines.OnUpdate(player);
			RunePrisons.OnUpdate(player);
			Chests.OnUpdate(player);
			Barrels.OnUpdate(player);
			Actors.OnUpdate(player);
#if DEBUG
			DebugDiagnostics.OnUpdate();
#endif
		}
    }
}
