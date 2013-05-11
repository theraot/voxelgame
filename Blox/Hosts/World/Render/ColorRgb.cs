using System.Diagnostics;

namespace Hexpoint.Blox.Hosts.World.Render
{
	/// <summary>Used for buffering color data to a VBO. Color component values can be 0-255.</summary>
	/// <remarks>Using byte uses the smallest amount of memory possible for color data.</remarks>
	internal struct ColorRgb
	{
		internal ColorRgb(byte red, byte green, byte blue) { R = red; G = green; B = blue; }
		internal ColorRgb(System.Drawing.Color color) { R = color.R; G = color.G; B = color.B; }
		internal byte R;
		internal byte G;
		internal byte B;
		internal const int SIZE = 3; //1 byte each
		internal float[] ToFloatArray()
		{
			return new[] { R / 255f, G / 255f, B / 255f };
		}

		public static ColorRgb operator *(ColorRgb colorRbg, float factor)
		{
			Debug.Assert(factor >= 0 && factor <= 1, "Can only multiply ColorRgb by value in the range 0-1");
			return new ColorRgb((byte)(colorRbg.R * factor), (byte)(colorRbg.G * factor), (byte)(colorRbg.B * factor));
		}
	}
}
