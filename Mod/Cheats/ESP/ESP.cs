using UnityEngine;

namespace Mod.Cheats.ESP
{
    internal enum EspStringStyle
    {
        Default = 0,
        Emphasized = 1
    }

    internal class LineDrawing
    {
        private readonly Vector3 start;
        private readonly Vector3 end;
        private readonly Color color;

        // constructor
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

    internal class StringDrawing
    {
        private readonly string text;
        private readonly Vector3 position;
        private readonly Color color;
        private readonly EspStringStyle style;

        // constructor
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
            foreach (var line in lineDrawings)
            {
                line.Draw();
            }

            foreach (var str in stringDrawings)
            {
                str.Draw();
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
			Items.OnUpdate();
			GoldPiles.OnUpdate();
			Shrines.OnUpdate();
			RunePrisons.OnUpdate();
			Chests.OnUpdate();
			Barrels.OnUpdate();
			Actors.OnUpdate();
#if DEBUG
			DebugDiagnostics.OnUpdate();
#endif
		}
    }
}
