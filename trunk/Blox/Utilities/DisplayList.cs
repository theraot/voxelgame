using System;
using System.Drawing;
using Hexpoint.Blox.Hosts.Ui;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Hosts.World.Render;
using Hexpoint.Blox.Textures;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Utilities
{
	internal static class DisplayList
	{
		public static void LoadDisplayLists()
		{
			BlockId = BlockRender.CreateBlockDisplayList();
			BlockHalfId = BlockRender.CreateBlockDisplayList(0.5f);
			BlockQuarterId = BlockRender.CreateBlockDisplayList(0.25f);
			ClutterId = CreateClutterDisplayList(1f);
			ClutterHalfId = CreateClutterDisplayList(0.5f);
			ClutterQuarterId = CreateClutterDisplayList(0.25f);

			SunId = CreateFaceDisplayList(0.1f);
			MoonId = CreateFaceDisplayList(0.04f);

			HeadId = CreateHeadDisplayList();
			TorsoId = CreateTorsoDisplayList();

			CompassId = CreateUiCenteredFaceDisplayList(16);
			UiButtonId = CreateUiFaceDisplayList(Buttons.BUTTON_SIZE, Buttons.BUTTON_SIZE);
			LargeCharId = CreateUiFaceDisplayList(TextureLoader.DEFAULT_FONT_LARGE_WIDTH, TextureLoader.DefaultFontLargeHeight);
			CreateCharAtlasDisplayLists(TextureLoader.DEFAULT_FONT_SMALL_WIDTH, TextureLoader.DefaultFontSmallHeight);

			//create text display lists (must come after character display lists have been created)
			TextF3ToHideId = CreateTextDisplayList("(F3 to hide)");
			TextPlayerId = CreateTextDisplayList("Player:");
			TextCursorId = CreateTextDisplayList("Cursor:");
			TextPerformanceId = CreateTextDisplayList("Performance:");
		}

		#region Display List Ids
		public static int BlockId;
		public static int BlockHalfId;
		public static int BlockQuarterId;
		public static int CompassId;
		public static int ClutterId;
		public static int ClutterHalfId;
		public static int ClutterQuarterId;

		public static int SunId;
		public static int MoonId;

		public static int HeadId;
		public static int TorsoId;

		public static int UiButtonId;
		public static int LargeCharId;
		public static int[] SmallCharAtlasIds;

		//text
		public static int TextF3ToHideId;
		public static int TextPlayerId;
		public static int TextCursorId;
		public static int TextPerformanceId;
		#endregion

		#region Rendering
		[Obsolete("Can just use GL.CallList() directly instead of this.")]
		public static void RenderDisplayList(int displayListId)
		{
			GL.CallList(displayListId);
		}

		/// <summary>Render display list using supplied block texture.</summary>
		public static void RenderDisplayList(int displayListId, BlockTextureType texture)
		{
			GL.BindTexture(TextureTarget.Texture2D, TextureLoader.GetBlockTexture(texture));
			GL.CallList(displayListId);
		}

		/// <summary>Render display list using supplied block texture and position.</summary>
		public static void RenderDisplayList(int displayListId, float x, float y, float z, BlockTextureType texture)
		{
			GL.BindTexture(TextureTarget.Texture2D, TextureLoader.GetBlockTexture(texture));
			GL.PushMatrix();
			GL.Translate(x, y, z);
			GL.CallList(displayListId);
			GL.PopMatrix();
		}

		/// <summary>Render display list using supplied environment texture and position.</summary>
		public static void RenderDisplayList(int displayListId, float x, float y, float z, EnvironmentTextureType texture)
		{
			GL.BindTexture(TextureTarget.Texture2D, TextureLoader.GetEnvironmentTexture(texture));
			GL.PushMatrix();

			GL.Translate(x, y, z);

			//rotate the face to directly face the player
			GL.Rotate(MathHelper.RadiansToDegrees(Game.Player.Coords.Direction) - 90, Vector3.UnitY);
			GL.Rotate(MathHelper.RadiansToDegrees(Game.Player.Coords.Pitch), Vector3.UnitX);
	
			GL.CallList(displayListId);
			GL.PopMatrix();
		}

		/// <summary>Render display list using supplied block texture and coords.</summary>
		[Obsolete("This isnt being used right now.")]public static void RenderDisplayList(int displayListId, ref Coords coords, BlockTextureType texture)
		{
			GL.BindTexture(TextureTarget.Texture2D, TextureLoader.GetBlockTexture(texture));
			GL.PushMatrix();
			GL.Translate(coords.Xf, coords.Yf, coords.Zf);
			GL.CallList(displayListId);
			GL.PopMatrix();
		}
		#endregion

		#region Create 3D Display Lists
		internal static void DeclareCuboidWithTexCoords(Vector3 startCorner, Vector3 endCorner)
		{
			//front
			GL.TexCoord2(1, 0); GL.Vertex3(startCorner.X, endCorner.Y, endCorner.Z);
			GL.TexCoord2(1, 1); GL.Vertex3(startCorner.X, startCorner.Y, endCorner.Z);
			GL.TexCoord2(0, 1); GL.Vertex3(endCorner.X, startCorner.Y, endCorner.Z);
			GL.TexCoord2(0, 0); GL.Vertex3(endCorner.X, endCorner.Y, endCorner.Z);

			//right
			GL.TexCoord2(1, 0); GL.Vertex3(endCorner.X, endCorner.Y, endCorner.Z);
			GL.TexCoord2(1, 1); GL.Vertex3(endCorner.X, startCorner.Y, endCorner.Z);
			GL.TexCoord2(0, 1); GL.Vertex3(endCorner.X, startCorner.Y, startCorner.Z);
			GL.TexCoord2(0, 0); GL.Vertex3(endCorner.X, endCorner.Y, startCorner.Z);

			//top
			GL.TexCoord2(1, 1); GL.Vertex3(startCorner.X, endCorner.Y, endCorner.Z);
			GL.TexCoord2(0, 1); GL.Vertex3(endCorner.X, endCorner.Y, endCorner.Z);
			GL.TexCoord2(0, 0); GL.Vertex3(endCorner.X, endCorner.Y, startCorner.Z);
			GL.TexCoord2(1, 0); GL.Vertex3(startCorner.X, endCorner.Y, startCorner.Z);

			//left
			GL.TexCoord2(0, 0); GL.Vertex3(startCorner.X, endCorner.Y, startCorner.Z);
			GL.TexCoord2(0, 1); GL.Vertex3(startCorner.X, startCorner.Y, startCorner.Z);
			GL.TexCoord2(1, 1); GL.Vertex3(startCorner.X, startCorner.Y, endCorner.Z);
			GL.TexCoord2(1, 0); GL.Vertex3(startCorner.X, endCorner.Y, endCorner.Z);

			//bottom
			GL.TexCoord2(0, 0); GL.Vertex3(endCorner.X, startCorner.Y, startCorner.Z);
			GL.TexCoord2(0, 1); GL.Vertex3(endCorner.X, startCorner.Y, endCorner.Z);
			GL.TexCoord2(1, 1); GL.Vertex3(startCorner.X, startCorner.Y, endCorner.Z);
			GL.TexCoord2(1, 0); GL.Vertex3(startCorner.X, startCorner.Y, startCorner.Z);

			//back
			GL.TexCoord2(0, 0); GL.Vertex3(endCorner.X, endCorner.Y, startCorner.Z);
			GL.TexCoord2(0, 1); GL.Vertex3(endCorner.X, startCorner.Y, startCorner.Z);
			GL.TexCoord2(1, 1); GL.Vertex3(startCorner.X, startCorner.Y, startCorner.Z);
			GL.TexCoord2(1, 0); GL.Vertex3(startCorner.X, endCorner.Y, startCorner.Z);
		}

		internal static void DeclareCuboidWithoutTexCoords(Vector3 startCorner, Vector3 endCorner)
		{
			//front
			GL.Vertex3(startCorner.X, endCorner.Y, endCorner.Z);
			GL.Vertex3(startCorner.X, startCorner.Y, endCorner.Z);
			GL.Vertex3(endCorner.X, startCorner.Y, endCorner.Z);
			GL.Vertex3(endCorner.X, endCorner.Y, endCorner.Z);

			//right
			GL.Vertex3(endCorner.X, endCorner.Y, endCorner.Z);
			GL.Vertex3(endCorner.X, startCorner.Y, endCorner.Z);
			GL.Vertex3(endCorner.X, startCorner.Y, startCorner.Z);
			GL.Vertex3(endCorner.X, endCorner.Y, startCorner.Z);

			//top
			GL.Vertex3(startCorner.X, endCorner.Y, endCorner.Z);
			GL.Vertex3(endCorner.X, endCorner.Y, endCorner.Z);
			GL.Vertex3(endCorner.X, endCorner.Y, startCorner.Z);
			GL.Vertex3(startCorner.X, endCorner.Y, startCorner.Z);

			//left
			GL.Vertex3(startCorner.X, endCorner.Y, startCorner.Z);
			GL.Vertex3(startCorner.X, startCorner.Y, startCorner.Z);
			GL.Vertex3(startCorner.X, startCorner.Y, endCorner.Z);
			GL.Vertex3(startCorner.X, endCorner.Y, endCorner.Z);

			//bottom
			GL.Vertex3(endCorner.X, startCorner.Y, startCorner.Z);
			GL.Vertex3(endCorner.X, startCorner.Y, endCorner.Z);
			GL.Vertex3(startCorner.X, startCorner.Y, endCorner.Z);
			GL.Vertex3(startCorner.X, startCorner.Y, startCorner.Z);

			//back
			GL.Vertex3(endCorner.X, endCorner.Y, startCorner.Z);
			GL.Vertex3(endCorner.X, startCorner.Y, startCorner.Z);
			GL.Vertex3(startCorner.X, startCorner.Y, startCorner.Z);
			GL.Vertex3(startCorner.X, endCorner.Y, startCorner.Z);
		}

		private static int CreateClutterDisplayList(float size)
		{
			if (size <= 0f || size > Constants.BLOCK_SIZE) throw new Exception("Size must be > 0 and < block size.");
			float offset = (Constants.BLOCK_SIZE - size) / 2; //offset in from the block edge depending on specified clutter size
			
			var id = GL.GenLists(1);
			GL.NewList(id, ListMode.Compile);

			GL.Begin(BeginMode.Quads);
			GL.TexCoord2(0, 1); GL.Vertex3(Constants.HALF_BLOCK_SIZE, 0f, offset); //between v2 - v3
			GL.TexCoord2(1, 1); GL.Vertex3(Constants.HALF_BLOCK_SIZE, 0f, Constants.BLOCK_SIZE - offset); //between v7 - v4
			GL.TexCoord2(1, 0); GL.Vertex3(Constants.HALF_BLOCK_SIZE, size, Constants.BLOCK_SIZE - offset); //between v6 - v5
			GL.TexCoord2(0, 0); GL.Vertex3(Constants.HALF_BLOCK_SIZE, size, offset); //between v1 - v0

			GL.TexCoord2(0, 1); GL.Vertex3(Constants.BLOCK_SIZE - offset, 0f, Constants.HALF_BLOCK_SIZE); //between v2 - v7
			GL.TexCoord2(1, 1); GL.Vertex3(offset, 0f, Constants.HALF_BLOCK_SIZE); //between v3 - v4
			GL.TexCoord2(1, 0); GL.Vertex3(offset, size, Constants.HALF_BLOCK_SIZE); //between v0 - v5
			GL.TexCoord2(0, 0); GL.Vertex3(Constants.BLOCK_SIZE - offset, size, Constants.HALF_BLOCK_SIZE); //between v1 - v6
			GL.End();

			GL.EndList();
			return id;
		}

		private static int CreateFaceDisplayList(float size)
		{
			var id = GL.GenLists(1);
			GL.NewList(id, ListMode.Compile);

			GL.Begin(BeginMode.Quads);
			GL.TexCoord2(1, 0); GL.Vertex3(size, size, 0);
			GL.TexCoord2(0, 0); GL.Vertex3(-size, size, 0);
			GL.TexCoord2(0, 1); GL.Vertex3(-size, -size, 0);
			GL.TexCoord2(1, 1); GL.Vertex3(size, -size, 0);
			GL.End();

			GL.EndList();
			return id;
		}

		private static int CreateHeadDisplayList()
		{
			var id = GL.GenLists(1);
			GL.NewList(id, ListMode.Compile);
			GL.Begin(BeginMode.Quads);

			GL.Color3(Color.PeachPuff);
			DeclareCuboidWithoutTexCoords(new Vector3(-0.27f, -0.27f, -0.27f), new Vector3(0.27f, 0.27f, 0.27f)); //head
			GL.Color3(Color.PapayaWhip);
			DeclareCuboidWithoutTexCoords(new Vector3(0.27f, -0.08f, -0.04f), new Vector3(0.34f, 0.04f, 0.04f)); //nose

			GL.Color3(Color.Black);
			DeclareCuboidWithoutTexCoords(new Vector3(0.275f, 0.10f, -0.14f), new Vector3(0.285f, 0.14f, -0.1f)); //pupil left
			DeclareCuboidWithoutTexCoords(new Vector3(0.275f, 0.10f, 0.1f), new Vector3(0.285f, 0.14f, 0.14f)); //pupil right

			GL.Color3(Color.White);
			DeclareCuboidWithoutTexCoords(new Vector3(0.27f, 0.06f, -0.2f), new Vector3(0.28f, 0.2f, -0.06f)); //eye left
			DeclareCuboidWithoutTexCoords(new Vector3(0.27f, 0.06f, 0.06f), new Vector3(0.28f, 0.2f, 0.2f)); //eye right
			DeclareCuboidWithoutTexCoords(new Vector3(0.27f, -0.19f, -0.2f), new Vector3(0.28f, -0.16f, 0.2f)); //mouth

			GL.End();
			GL.EndList();
			return id;
		}

		private static int CreateTorsoDisplayList()
		{
			var id = GL.GenLists(1);
			GL.NewList(id, ListMode.Compile);
			GL.Begin(BeginMode.Quads);

			GL.Color3(Color.PeachPuff);
			DeclareCuboidWithoutTexCoords(new Vector3(-0.15f, 1.2f, -0.15f), new Vector3(0.15f, 1.4f, 0.15f)); //neck

			GL.Color3(Color.IndianRed);
			DeclareCuboidWithoutTexCoords(new Vector3(-0.22f, 0.6f, -0.35f), new Vector3(0.22f, 1.2f, 0.35f)); //chest
			GL.Color3(Color.DarkSeaGreen);
			DeclareCuboidWithoutTexCoords(new Vector3(-0.15f, 0.65f, -0.48f), new Vector3(0.15f, 1.2f, -0.351f)); //arm
			DeclareCuboidWithoutTexCoords(new Vector3(-0.15f, 0.65f, 0.351f), new Vector3(0.15f, 1.2f, 0.48f)); //arm

			GL.Color3(Color.Navy);
			DeclareCuboidWithoutTexCoords(new Vector3(-0.18f, 0f, -0.35f), new Vector3(0.18f, 0.6f, -0.05f)); //leg
			DeclareCuboidWithoutTexCoords(new Vector3(-0.18f, 0f, 0.05f), new Vector3(0.18f, 0.6f, 0.35f)); //leg

			GL.End();
			GL.EndList();
			return id;
		}
		#endregion

		#region Create 2D Display Lists (UI)
		/// <summary>
		/// Create a UI 2D display list for a single face. Used for Ui chars, Ui buttons, etc.
		/// To use: Translate to the top left corner and pre-bind the applicable texture.
		/// </summary>
		private static int CreateUiFaceDisplayList(int width, int height)
		{
			var id = GL.GenLists(1);
			GL.NewList(id, ListMode.Compile);

			GL.Begin(BeginMode.Quads);
			GL.TexCoord2(1, 0); GL.Vertex2(width, 0);
			GL.TexCoord2(0, 0); GL.Vertex2(0, 0);
			GL.TexCoord2(0, 1); GL.Vertex2(0, height);
			GL.TexCoord2(1, 1); GL.Vertex2(width, height);
			GL.End();

			GL.EndList();
			return id;
		}

		/// <summary>
		/// Create a UI 2D display list for a single centered face drawn out from the center.
		/// Use this for UI elements that need to be rotated as its much easier if they are drawn centered. ie: compass
		/// </summary>
		private static int CreateUiCenteredFaceDisplayList(int size)
		{
			float half = size / 2f;
			var id = GL.GenLists(1);
			GL.NewList(id, ListMode.Compile);

			GL.Begin(BeginMode.Quads);
			GL.TexCoord2(1, 0); GL.Vertex2(half, -half);
			GL.TexCoord2(0, 0); GL.Vertex2(-half, -half);
			GL.TexCoord2(0, 1); GL.Vertex2(-half, half);
			GL.TexCoord2(1, 1); GL.Vertex2(half, half);
			GL.End();

			GL.EndList();
			return id;
		}

		/// <summary>
		/// Create a display list for every displayable character using the supplied dimensions.
		/// Tex coords are set according to the characters position in the texture atlas.
		/// </summary>
		private static void CreateCharAtlasDisplayLists(int width, int height)
		{
			SmallCharAtlasIds = new int[Constants.HIGHEST_ASCII_CHAR + 1];
			int firstListId = GL.GenLists(Constants.TOTAL_ASCII_CHARS); //get the first id in a contiguous set
			for (var i = Constants.LOWEST_ASCII_CHAR; i <= Constants.HIGHEST_ASCII_CHAR; i++)
			{
				int atlasPosition = i - Constants.LOWEST_ASCII_CHAR;
				int listId = firstListId + atlasPosition;
				SmallCharAtlasIds[i] = listId; //assign the display list id for this char to the corresponding position in the lookup array
				GL.NewList(listId, ListMode.Compile);
				GL.Begin(BeginMode.Quads);
				GL.TexCoord2(Constants.CHAR_ATLAS_RATIO * (atlasPosition + 1), 0d); GL.Vertex2(width, 0);
				GL.TexCoord2(Constants.CHAR_ATLAS_RATIO * atlasPosition, 0d); GL.Vertex2(0, 0);
				GL.TexCoord2(Constants.CHAR_ATLAS_RATIO * atlasPosition, 1d); GL.Vertex2(0, height);
				GL.TexCoord2(Constants.CHAR_ATLAS_RATIO * (atlasPosition + 1), 1d); GL.Vertex2(width, height);
				GL.End();
				GL.EndList();
			}
		}

		/// <summary>Create a display list using the supplied text.</summary>
		private static int CreateTextDisplayList(string text)
		{
			var id = GL.GenLists(1);
			GL.NewList(id, ListMode.Compile);
			foreach (char c in text)
			{
				GL.CallList(SmallCharAtlasIds[c]);
				GL.Translate(TextureLoader.DEFAULT_FONT_SMALL_WIDTH, 0, 0);
			}
			GL.EndList();
			return id;
		}
		#endregion

		#region Delete Display Lists
		public static void DeleteDisplayLists()
		{
			GL.DeleteLists(BlockId, 1);
			GL.DeleteLists(BlockHalfId, 1);
			GL.DeleteLists(BlockQuarterId, 1);
			GL.DeleteLists(CompassId, 1);
			GL.DeleteLists(ClutterId, 1);
			GL.DeleteLists(ClutterHalfId, 1);
			GL.DeleteLists(ClutterQuarterId, 1);

			GL.DeleteLists(UiButtonId, 1);
			GL.DeleteLists(LargeCharId, 1);
			GL.DeleteLists(SmallCharAtlasIds[Constants.LOWEST_ASCII_CHAR], Constants.TOTAL_ASCII_CHARS);

			GL.DeleteLists(TextF3ToHideId, 1);
			GL.DeleteLists(TextPlayerId, 1);
			GL.DeleteLists(TextCursorId, 1);
			GL.DeleteLists(TextPerformanceId, 1);
		}
		#endregion

	}
}
