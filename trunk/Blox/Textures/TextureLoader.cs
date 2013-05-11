using System;
using System.Drawing;
using Hexpoint.Blox.Textures.Resources;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Textures
{
	#region Texture Enums
	/// <summary>Block texture index. The order can be changed without affecting anything, so keep this alphabetical with Air always at 0 and water last.</summary>
	public enum BlockTextureType
	{
		Air, //not actually a texture, nothing will be rendered
		Bricks,
		Coal,
		Cobble,
		Copper,
		Crate,
		CrateSide,
		Dirt,
		ElmTree,
		FancyBlack,
		FancyGreen,
		FancyRed,
		FancyWhite,
		Gold,
		Grass,
		GrassSide,
		Gravel,
		Ice,
		Iron,
		Lava,
		LavaRock,
		Leaves,
		Oil,
		PrisonBars,
		Sand,
		SandDark,
		Shelf1,
		Snow,
		SnowLeaves,
		SnowSide,
		Speaker,
		SteelDoorBottom,
		SteelDoorTop,
		SteelPlate,
		SteelPlate2,
		Rock,
		Tree,
		TreeTrunk,
		WoodTile1,
		WoodTile2,
		/// <summary>First texture in the water animation. All water VBOs are always assigned to this texture id.</summary>
		Water,
		/// <summary>Used for water animation only. No VBO will be assigned to this texture id.</summary>
		Water2,
		/// <summary>Used for water animation only. No VBO will be assigned to this texture id.</summary>
		Water3,
		/// <summary>Used for water animation only. No VBO will be assigned to this texture id.</summary>
		Water4
	}

	/// <summary>Clutter texture index. The order can be changed without affecting anything.</summary>
	public enum ClutterTextureType
	{
		Bush,
		Grass1,
		Grass2,
		Grass3,
		Grass4,
		Grass5,
		Grass6,
		Grass7,
		Grass8
	}

	public enum EnvironmentTextureType
	{
		Sun,
		Moon
	}

	/// <summary>All items regardless of type (light sources, etc.) go in this enum so they can share the same resource file. The order can be changed without affecting anything.</summary>
	public enum ItemTextureType
	{
		Lantern
	}

	public enum UiTextureType
	{
		CompassArrow,
		BlockCursor,
		ToolDefault,
		ToolCuboid,
		ToolFastBuild,
		ToolFastDestroy,
		ToolTree,
		CrossHairs,
		Axe,
		Shovel,
		PickAxe,
		Tower,
		SmallKeep,
		LargeKeep
	}

	/// <summary>All units regardless of type (players, mobs etc.) go in this enum so they can share the same resource file. The order can be changed without affecting anything.</summary>
	public enum UnitTextureType
	{
		
	}
	#endregion

	internal static class TextureLoader
	{
		public static void Load()
		{
			LoadBlockTextures();
			LoadClutterTextures();
			LoadEnvironmentTextures();
			LoadItemTextures();
			LoadUiTextures();
			LoadUnitTextures();

			LoadCharacterTextures(CharacterTexturesLarge, DefaultFontLarge, DEFAULT_FONT_LARGE_WIDTH, DefaultFontLargeHeight);
			CharacterAtlasSmall = LoadCharacterAtlas(DefaultFontSmall, DEFAULT_FONT_SMALL_WIDTH, DefaultFontSmallHeight);
			//CharacterAtlasLarge = LoadCharacterAtlas(DefaultFontLarge, DEFAULT_FONT_LARGE_WIDTH, DefaultFontLargeHeight);
		}

		/// <summary>Get a texture from a supplied bitmap. The bitmap will typically come from a resource file.</summary>
		/// <param name="bitmap">bitmap image to create texture from</param>
		/// <param name="createMipmap">should mipmaps be created for this texture</param>
		/// <param name="blur">should this texture have blur (magnification filter linear)</param>
		/// <param name="clamp">should this texture be clamped</param>
		/// <returns>integer id of the texture</returns>
		private static int GetTextureFromBitmap(Bitmap bitmap, bool createMipmap = true, bool blur = true, bool clamp = false)
		{
			var id = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, id);

			//http://www.opengl.org/sdk/docs/man/xhtml/glTexParameter.xml
			//GL_NEAREST is generally faster than GL_LINEAR, but it can produce textured images with sharper edges because the transition between texture elements is not as smooth
			if (createMipmap && Config.Mipmapping)
			{
				//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearetMipmapLinear); //minification filter
				//gm: trying dual linear now, i dont notice a performance change and its supposed to look better
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); //minification filter
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1); //gm: requires OpenGL 1.4+
			}
			else
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear); //minification filter
			}

			//mipmapping is not applicable to the magnification filter
			//gm: linear makes the textures look much better close up with a bluring effect, fps change wasnt noticable
			//	-wouldnt want linear on everything, block cursor for example, probably not on ui chars? blurs the nameplates too much, not on ui buttons
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, blur && Config.LinearMagnificationFilter ? (int)TextureMagFilter.Linear : (int)TextureMagFilter.Nearest); //magnification filter

			if (clamp)
			{
				//GL_CLAMP_TO_EDGE was introduced in OpenGL 1.2, dont blend with neighbors, prevents edge lines for textures with drastic differences on opposite edges
				//-should be used with ui buttons, text chars, terrain like GrassSide and SnowSide, clutter, etc.
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			}
			else
			{
				//GL_REPEAT is the default, allows the texture to blend nicely with neighbors, especially neighbors with the same texture
				//-should be used with terrain like Grass, Snow, Sand, Tree, etc.
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
			}

			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
			bitmap.UnlockBits(bitmapData);

			return id;
		}

		#region Character Textures
		public const int DEFAULT_FONT_SMALL_WIDTH = 11;
		public static readonly Font DefaultFontSmall = new Font("Lucida Console", DEFAULT_FONT_SMALL_WIDTH, FontStyle.Regular);
		public static readonly int DefaultFontSmallHeight = DefaultFontSmall.Height; //calculating this was actually taking a lot of cpu time, zz

		public const int DEFAULT_FONT_LARGE_WIDTH = 64;
		public static readonly Font DefaultFontLarge = new Font("Lucida Console", DEFAULT_FONT_LARGE_WIDTH, FontStyle.Regular);
		public static readonly int DefaultFontLargeHeight = DefaultFontLarge.Height; //calculating this was actually taking a lot of cpu time, zz
		private static readonly int[] CharacterTexturesLarge = new int[Constants.HIGHEST_ASCII_CHAR + 1];

		internal static int CharacterAtlasSmall;
		//internal static int CharacterAtlasLarge;

		private static int LoadCharacterAtlas(Font font, int width, int height)
		{
			//this block changes the tree tool texture to prove the TexSubImage works at least by putting 3 crosshairs on top of the existing texture
			//var crossHairs = UiTextures.CrossHairs;
			//GL.BindTexture(TextureTarget.Texture2D, GetUiTexture(UiTextureType.ToolTree));
			//var crossHairsData = crossHairs.LockBits(new Rectangle(0, 0, crossHairs.Width, crossHairs.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			//GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 16, 16, PixelFormat.Bgra, PixelType.UnsignedByte, crossHairsData.Scan0);
			//GL.TexSubImage2D(TextureTarget.Texture2D, 0, 16, 16, 16, 16, PixelFormat.Bgra, PixelType.UnsignedByte, crossHairsData.Scan0);
			//GL.TexSubImage2D(TextureTarget.Texture2D, 0, 32, 0, 16, 16, PixelFormat.Bgra, PixelType.UnsignedByte, crossHairsData.Scan0);
			//crossHairs.UnlockBits(crossHairsData);

			var textureAtlasId = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, textureAtlasId);
			//need to set the min/max filters, they dont seem to get defaults
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			//allocate space for the atlas
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width * Constants.TOTAL_ASCII_CHARS, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

			for (var i = Constants.LOWEST_ASCII_CHAR; i <= Constants.HIGHEST_ASCII_CHAR; i++)
			{
				var bitmap = new Bitmap(width, height);
				var graphics = Graphics.FromImage(bitmap);
				graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
				graphics.DrawString(Convert.ToChar(i).ToString(), font, Brushes.White, new Point(-1, 0));

				var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, (i - Constants.LOWEST_ASCII_CHAR) * bitmap.Width, 0, bitmapData.Width, bitmapData.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
				bitmap.UnlockBits(bitmapData);
			}

			return textureAtlasId;
		}

		private static void LoadCharacterTextures(int[] textures, Font font, int width, int height)
		{
			for (var i = Constants.LOWEST_ASCII_CHAR; i <= Constants.HIGHEST_ASCII_CHAR; i++)
			{
				var texture = new Bitmap(width, height);
				var graphics = Graphics.FromImage(texture);
				graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
				graphics.DrawString(Convert.ToChar(i).ToString(), font, Brushes.White, new Point(-1, 0));

				textures[i] = GetTextureFromBitmap(texture, false, false, true);
				texture.Dispose();
			}
		}

		public static int GetLargeCharacterTexture(char texture)
		{
			return CharacterTexturesLarge[texture];
		}
		#endregion

		#region Block Textures
		private static void LoadBlockTextures()
		{
			_blockTextures = new int[Enum.GetNames(typeof(BlockTextureType)).Length];
			_blockTextures[(int)BlockTextureType.Bricks] = GetTextureFromBitmap(BlockTextures64.Bricks);
			_blockTextures[(int)BlockTextureType.Coal] = GetTextureFromBitmap(BlockTextures64.Coal);
			_blockTextures[(int)BlockTextureType.Cobble] = GetTextureFromBitmap(BlockTextures64.Cobble);
			_blockTextures[(int)BlockTextureType.Copper] = GetTextureFromBitmap(BlockTextures64.Copper);
			_blockTextures[(int)BlockTextureType.Crate] = GetTextureFromBitmap(BlockTextures64.Crate);
			_blockTextures[(int)BlockTextureType.CrateSide] = GetTextureFromBitmap(BlockTextures64.CrateSide);
			_blockTextures[(int)BlockTextureType.Dirt] = GetTextureFromBitmap(BlockTextures64.Dirt);
			_blockTextures[(int)BlockTextureType.ElmTree] = GetTextureFromBitmap(BlockTextures64.ElmTree);
			_blockTextures[(int)BlockTextureType.FancyBlack] = GetTextureFromBitmap(BlockTextures64.FancyBlack);
			_blockTextures[(int)BlockTextureType.FancyGreen] = GetTextureFromBitmap(BlockTextures64.FancyGreen);
			_blockTextures[(int)BlockTextureType.FancyRed] = GetTextureFromBitmap(BlockTextures64.FancyRed);
			_blockTextures[(int)BlockTextureType.FancyWhite] = GetTextureFromBitmap(BlockTextures64.FancyWhite);
			_blockTextures[(int)BlockTextureType.Gold] = GetTextureFromBitmap(BlockTextures64.Gold);
			_blockTextures[(int)BlockTextureType.Grass] = GetTextureFromBitmap(BlockTextures64.Grass);
			_blockTextures[(int)BlockTextureType.GrassSide] = GetTextureFromBitmap(BlockTextures64.GrassSide, clamp:true);
			_blockTextures[(int)BlockTextureType.Gravel] = GetTextureFromBitmap(BlockTextures64.Gravel);
			_blockTextures[(int)BlockTextureType.Ice] = GetTextureFromBitmap(BlockTextures64.Ice);
			_blockTextures[(int)BlockTextureType.Iron] = GetTextureFromBitmap(BlockTextures64.Iron);
			_blockTextures[(int)BlockTextureType.Lava] = GetTextureFromBitmap(BlockTextures64.Lava);
			_blockTextures[(int)BlockTextureType.LavaRock] = GetTextureFromBitmap(BlockTextures64.LavaRock);
			_blockTextures[(int)BlockTextureType.Leaves] = GetTextureFromBitmap(BlockTextures64.Leaves);
			_blockTextures[(int)BlockTextureType.Oil] = GetTextureFromBitmap(BlockTextures64.Oil);
			_blockTextures[(int)BlockTextureType.PrisonBars] = GetTextureFromBitmap(BlockTextures64.PrisonBars);
			_blockTextures[(int)BlockTextureType.Rock] = GetTextureFromBitmap(BlockTextures64.Rock);
			_blockTextures[(int)BlockTextureType.Sand] = GetTextureFromBitmap(BlockTextures64.Sand);
			_blockTextures[(int)BlockTextureType.SandDark] = GetTextureFromBitmap(BlockTextures64.SandDark);
			_blockTextures[(int)BlockTextureType.Shelf1] = GetTextureFromBitmap(BlockTextures64.Shelf1);
			_blockTextures[(int)BlockTextureType.Snow] = GetTextureFromBitmap(BlockTextures64.Snow);
			_blockTextures[(int)BlockTextureType.SnowLeaves] = GetTextureFromBitmap(BlockTextures64.SnowLeaves);
			_blockTextures[(int)BlockTextureType.SnowSide] = GetTextureFromBitmap(BlockTextures64.SnowSide, clamp:true);
			_blockTextures[(int)BlockTextureType.Speaker] = GetTextureFromBitmap(BlockTextures64.Speaker);
			_blockTextures[(int)BlockTextureType.SteelDoorBottom] = GetTextureFromBitmap(BlockTextures64.SteelDoorBottom, clamp:true);
			_blockTextures[(int)BlockTextureType.SteelDoorTop] = GetTextureFromBitmap(BlockTextures64.SteelDoorTop, clamp:true);
			_blockTextures[(int)BlockTextureType.SteelPlate] = GetTextureFromBitmap(BlockTextures64.SteelPlate);
			_blockTextures[(int)BlockTextureType.SteelPlate2] = GetTextureFromBitmap(BlockTextures64.SteelPlate2);
			_blockTextures[(int)BlockTextureType.Tree] = GetTextureFromBitmap(BlockTextures64.Tree);
			_blockTextures[(int)BlockTextureType.TreeTrunk] = GetTextureFromBitmap(BlockTextures64.TreeTrunk);
			_blockTextures[(int)BlockTextureType.WoodTile1] = GetTextureFromBitmap(BlockTextures64.WoodTile1);
			_blockTextures[(int)BlockTextureType.WoodTile2] = GetTextureFromBitmap(BlockTextures64.WoodTile2);
			_blockTextures[(int)BlockTextureType.Water] = GetTextureFromBitmap(BlockTextures64.Water);
			_blockTextures[(int)BlockTextureType.Water2] = GetTextureFromBitmap(BlockTextures64.Water2);
			_blockTextures[(int)BlockTextureType.Water3] = GetTextureFromBitmap(BlockTextures64.Water3);
			_blockTextures[(int)BlockTextureType.Water4] = GetTextureFromBitmap(BlockTextures64.Water4);
		}

		private static int[] _blockTextures;
		public static int GetBlockTexture(BlockTextureType texture)
		{
			return _blockTextures[(int)texture];
		}
		#endregion

		#region Clutter Textures
		private static void LoadClutterTextures()
		{
			_clutterTextures = new int[Enum.GetNames(typeof(ClutterTextureType)).Length];
			_clutterTextures[(int)ClutterTextureType.Bush] = GetTextureFromBitmap(ClutterTextures64.Bush, clamp: true);
			_clutterTextures[(int)ClutterTextureType.Grass1] = GetTextureFromBitmap(ClutterTextures64.Grass1, clamp: true);
			_clutterTextures[(int)ClutterTextureType.Grass2] = GetTextureFromBitmap(ClutterTextures64.Grass2, clamp: true);
			_clutterTextures[(int)ClutterTextureType.Grass3] = GetTextureFromBitmap(ClutterTextures64.Grass3, clamp: true);
			_clutterTextures[(int)ClutterTextureType.Grass4] = GetTextureFromBitmap(ClutterTextures64.Grass4, clamp: true);
			_clutterTextures[(int)ClutterTextureType.Grass5] = GetTextureFromBitmap(ClutterTextures64.Grass5, clamp: true);
			_clutterTextures[(int)ClutterTextureType.Grass6] = GetTextureFromBitmap(ClutterTextures64.Grass6, clamp: true);
			_clutterTextures[(int)ClutterTextureType.Grass7] = GetTextureFromBitmap(ClutterTextures64.Grass7, clamp: true);
			_clutterTextures[(int)ClutterTextureType.Grass8] = GetTextureFromBitmap(ClutterTextures64.Grass8, clamp: true);
		}

		private static int[] _clutterTextures;
		public static int GetClutterTexture(ClutterTextureType texture)
		{
			return _clutterTextures[(int)texture];
		}
		#endregion

		#region Environment Textures
		private static void LoadEnvironmentTextures()
		{
			_environmentTextures = new int[Enum.GetNames(typeof(EnvironmentTextureType)).Length];
			_environmentTextures[(int)EnvironmentTextureType.Sun] = GetTextureFromBitmap(EnvironmentTextures.Sun, false, true, true);
			_environmentTextures[(int)EnvironmentTextureType.Moon] = GetTextureFromBitmap(EnvironmentTextures.Moon, false, true, true);
		}

		private static int[] _environmentTextures;
		public static int GetEnvironmentTexture(EnvironmentTextureType texture)
		{
			return _environmentTextures[(int)texture];
		}
		#endregion

		#region Item Textures
		private static void LoadItemTextures()
		{
			_itemTextures = new int[Enum.GetNames(typeof(ItemTextureType)).Length];
			_itemTextures[(int)ItemTextureType.Lantern] = GetTextureFromBitmap(ItemTextures.Lantern, clamp: true);
		}

		private static int[] _itemTextures;
		public static int GetItemTexture(ItemTextureType texture)
		{
			return _itemTextures[(int)texture];
		}
		#endregion

		#region Ui Textures
		private static void LoadUiTextures()
		{
			_uiTextures = new int[Enum.GetNames(typeof(UiTextureType)).Length];
			_uiTextures[(int)UiTextureType.CompassArrow] = GetTextureFromBitmap(UiTextures.CompassArrow, false, false, true);
			_uiTextures[(int)UiTextureType.BlockCursor] = GetTextureFromBitmap(UiTextures.BlockCursor, false, false, true);
			_uiTextures[(int)UiTextureType.ToolDefault] = GetTextureFromBitmap(UiTextures.ToolDefault, false, false, true);
			_uiTextures[(int)UiTextureType.ToolCuboid] = GetTextureFromBitmap(UiTextures.ToolCuboid, false, false, true);
			_uiTextures[(int)UiTextureType.ToolFastBuild] = GetTextureFromBitmap(UiTextures.ToolFastBuild, false, false, true);
			_uiTextures[(int)UiTextureType.ToolFastDestroy] = GetTextureFromBitmap(UiTextures.ToolFastDestroy, false, false, true);
			_uiTextures[(int)UiTextureType.ToolTree] = GetTextureFromBitmap(UiTextures.ToolTree, false, false, true);
			_uiTextures[(int)UiTextureType.CrossHairs] = GetTextureFromBitmap(UiTextures.CrossHairs, false, false, true);
			_uiTextures[(int)UiTextureType.Axe] = GetTextureFromBitmap(UiTextures.Axe, false, false, true);
			_uiTextures[(int)UiTextureType.Shovel] = GetTextureFromBitmap(UiTextures.Shovel, false, false, true);
			_uiTextures[(int)UiTextureType.PickAxe] = GetTextureFromBitmap(UiTextures.PickAxe, false, false, true);
			_uiTextures[(int)UiTextureType.Tower] = GetTextureFromBitmap(UiTextures.Tower, false, false, true);
			_uiTextures[(int)UiTextureType.SmallKeep] = GetTextureFromBitmap(UiTextures.SmallKeep, false, false, true);
			_uiTextures[(int)UiTextureType.LargeKeep] = GetTextureFromBitmap(UiTextures.LargeKeep, false, false, true);
		}

		private static int[] _uiTextures;
		public static int GetUiTexture(UiTextureType texture)
		{
			return _uiTextures[(int)texture];
		}
		#endregion

		#region Unit Textures
		private static void LoadUnitTextures()
		{
			_unitTextures = new int[Enum.GetNames(typeof(UnitTextureType)).Length];
		}

		private static int[] _unitTextures;
		public static int GetUnitTexture(UnitTextureType texture)
		{
			return _unitTextures[(int)texture];
		}
		#endregion
	}
}
