using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Utilities
{
	internal static class GlHelper
	{
		//gm: might use something like this down the road for preventing redundant GL calls between hosts
		//private static bool _blend;
		//internal static void Blend(bool blend)
		//{
		//    if (blend == _blend) return; //already set, no need to make GL call
		//    _blend = blend;
		//    if (blend) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
		//}

		/// <summary>Reset color to solid white so subsequent polygons/textures are not affected by color.</summary>
		internal static void ResetColor()
		{
			GL.Color3(Color.White);
		}

		/// <summary>Unbind the current texture to allow drawing color filled polygons.</summary>
		internal static void ResetTexture()
		{
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		[Obsolete("Not using this right now.")]
		internal static void DrawSphere(int lats, int longs, Color color1, Color color2)
		{
			const int SIZE = 100;
			for (int i = 0; i <= lats; i++)
			{
				double lat0 = Math.PI * (-0.5 + (double)(i - 1) / lats);
				double z0 = Math.Sin(lat0);
				double zr0 = Math.Cos(lat0);

				double lat1 = Math.PI * (-0.5 + (double)i / lats);
				double z1 = Math.Sin(lat1);
				double zr1 = Math.Cos(lat1);

				GL.Color3(i % 2 == 0 ? color1 : color2);

				GL.Begin(BeginMode.QuadStrip);
				for (int j = 0; j <= longs; j++)
				{
					double lng = 2 * Math.PI * (j - 1) / longs;
					double x = Math.Cos(lng);
					double y = Math.Sin(lng);

					//GL.Normal3(x * zr0, y * zr0 - SIZE, z0);
					GL.Vertex3(x * zr0, y * zr0 - SIZE, z0);
					//GL.Normal3(x * zr1, y * zr1 - SIZE, z1);
					GL.Vertex3(x * zr1, y * zr1 - SIZE, z1);
				}
				GL.End();
			}
		}
	}
}
