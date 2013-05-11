// CUBE VERTICES (always draw ccw starting at top right)
//    v6----- v5
//   /|      /|
//  v1------v0|
//  | |     | |
//  | |v7---|-|v4
//  |/      |/
//  v2------v3
/////////////////

//SMOOTH LIGHTING MAP (block 4 is used by all 4 vertices, blocks 1,3,5,7 are used by 2 vertices each, blocks 0,2,6,8 are used by one vertex only)
//	0  1  2
//   v1 v0
//	3  4  5
//   v2 v3
//	6  7  8
/////////////////

using System;
using Hexpoint.Blox.Utilities;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Hosts.World.Render
{
	internal static class BlockRender
	{
		private static readonly TexCoordsShort[] TexCoords = new[] { new TexCoordsShort(1, 0), new TexCoordsShort(0, 0), new TexCoordsShort(0, 1), new TexCoordsShort(1, 1) };

		#region Add Block Face To VBO
		/// <summary>Add a block face to a VBO using smooth shade lighting. Each vertex has a unique lighting color.</summary>
		public static void AddBlockFaceToVbo(ChunkVbo chunkVbo, Face face, float x, float y, float z, byte v0Color, byte v1Color, byte v2Color, byte v3Color)
		{
			AddBlockFaceToVbo(chunkVbo, face, x, y, z, new[] { new ColorRgb(v0Color, v0Color, v0Color), new ColorRgb(v1Color, v1Color, v1Color), new ColorRgb(v2Color, v2Color, v2Color), new ColorRgb(v3Color, v3Color, v3Color) });
		}

		/// <summary>Add a block face to a VBO using flat lighting. Same light color is used for all 4 vertices.</summary>
		public static void AddBlockFaceToVbo(ChunkVbo chunkVbo, Face face, float x, float y, float z, byte lightColor)
		{
			var colorRgb = new ColorRgb(lightColor, lightColor, lightColor);
			AddBlockFaceToVbo(chunkVbo, face, x, y, z, new[] {colorRgb, colorRgb, colorRgb, colorRgb});
		}

		private static void AddBlockFaceToVbo(ChunkVbo chunkVbo, Face face, float x0, float y0, float z0, ColorRgb[] colors)
		{
			//position calculations
			float x1 = x0 + 1f; //x starting left at 0 in block
			float y1 = y0 + 1f; //y starting bottom at 0 in block
			float z1 = z0 + 1f; //z starting back at 0 in block

			switch (face)
			{
				case Face.Front:
					// v0-v1-v2-v3 (front)
					chunkVbo.Positions.Add(new Vector3(x1, y1, z1));
					chunkVbo.Positions.Add(new Vector3(x0, y1, z1));
					chunkVbo.Positions.Add(new Vector3(x0, y0, z1));
					chunkVbo.Positions.Add(new Vector3(x1, y0, z1));
					break;
				case Face.Right:
					// v5-v0-v3-v4 (right)
					chunkVbo.Positions.Add(new Vector3(x1, y1, z0));
					chunkVbo.Positions.Add(new Vector3(x1, y1, z1));
					chunkVbo.Positions.Add(new Vector3(x1, y0, z1));
					chunkVbo.Positions.Add(new Vector3(x1, y0, z0));
					break;
				case Face.Top:
					// v5-v6-v1-v0 (top)
					chunkVbo.Positions.Add(new Vector3(x1, y1, z0));
					chunkVbo.Positions.Add(new Vector3(x0, y1, z0));
					chunkVbo.Positions.Add(new Vector3(x0, y1, z1));
					chunkVbo.Positions.Add(new Vector3(x1, y1, z1));
					break;
				case Face.Left:
					// v1-v6-v7-v2 (left)
					chunkVbo.Positions.Add(new Vector3(x0, y1, z1));
					chunkVbo.Positions.Add(new Vector3(x0, y1, z0));
					chunkVbo.Positions.Add(new Vector3(x0, y0, z0));
					chunkVbo.Positions.Add(new Vector3(x0, y0, z1));
					break;
				case Face.Bottom:
					// v3-v2-v7-v4 (bottom)
					chunkVbo.Positions.Add(new Vector3(x1, y0, z1));
					chunkVbo.Positions.Add(new Vector3(x0, y0, z1));
					chunkVbo.Positions.Add(new Vector3(x0, y0, z0));
					chunkVbo.Positions.Add(new Vector3(x1, y0, z0));
					break;
				case Face.Back:
					// v6-v5-v4-v7 (back)
					chunkVbo.Positions.Add(new Vector3(x0, y1, z0));
					chunkVbo.Positions.Add(new Vector3(x1, y1, z0));
					chunkVbo.Positions.Add(new Vector3(x1, y0, z0));
					chunkVbo.Positions.Add(new Vector3(x0, y0, z0));
					break;
			}

			if (Settings.OutlineChunks) //highlights chunk edges in yellow, the actual chunk edge line is the line between the 2 yellow block strips (use /outline to toggle)
			{
				var rgbYellow = new ColorRgb(255, 255, 50);
				chunkVbo.Colors.AddRange(WorldData.IsOnChunkBorder((int)x0, (int)z0) ? new[] {rgbYellow, rgbYellow, rgbYellow, rgbYellow} : colors);
			}
			else
			{
				chunkVbo.Colors.AddRange(colors);
			}
			chunkVbo.TexCoords.AddRange(TexCoords);
		}
		#endregion

		#region Display Lists
		/// <summary>Creates a block display list of supplied size.</summary>
		/// <param name="size">size should be > 0, 1 is a full size block, 0.5 is a half size block etc.</param>
		/// <returns>display list id</returns>
		public static int CreateBlockDisplayList(float size = 1f)
		{
			if (size <= 0) throw new Exception("Size must be > 0.");
			
			var id = GL.GenLists(1);
			GL.NewList(id, ListMode.Compile);
			GL.Begin(BeginMode.Quads);

			DisplayList.DeclareCuboidWithTexCoords(new Vector3(-size / 2, 0f, -size / 2), new Vector3(size / 2, size, size / 2));

			GL.End();
			GL.EndList();
			return id;
		}
		#endregion
	}
}
