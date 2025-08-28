using UnityEngine;

namespace Mod.Cheats.ESP
{
    internal class LineDrawing
    {
        private Vector3 start;
        private Vector3 end;
        private Color color;

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
        private string text;
        private Vector3 position;
        private Color color;

        // constructor
        public StringDrawing(string text, Vector3 position, Color color)
        {
            this.text = text;
            this.position = position;
            this.color = color;
        }

        public void Draw()
        {
            Drawing.DrawString(position, text, color);
        }
    }

    internal class ESP
    {
        public static List<LineDrawing> lineDrawings = new List<LineDrawing>();
        public static List<StringDrawing> stringDrawings = new List<StringDrawing>();

        public static void AddLine(Vector3 start, Vector3 end, Color color)
        {
            lineDrawings.Add(new LineDrawing(start, end, color));
        }

        public static void AddString(string text, Vector3 position, Color color)
        {
            stringDrawings.Add(new StringDrawing(text, position, color));
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
			Actors.OnUpdate();
		}
    }
}
