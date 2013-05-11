using System.Drawing;
using Hexpoint.Blox.Hosts.World;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Hosts.Ui
{
	internal enum ButtonType { Action, GridPicker, Tool }
	/// <summary>Tools for right side bar. Only available in creative mode. Keep order same as on screen.</summary>
	internal enum ToolType { Default, ToolBlockType, Cuboid, FastBuild, FastDestroy, Tree, Tower, SmallKeep, LargeKeep }
	
	internal class Button
	{
		public Button(ButtonType type, int x, int y)
		{
			Type = type;
			X = x;
			Y = y;
		}

		public Button(ButtonType type, int x, int y, int texture) : this(type, x, y)
		{
			Texture = texture;
		}

		public int X;
		public int Y;
		public int Texture;
		public ButtonType Type;
		public char KeyBind;
		public bool Hightlight;
		//public bool HighlightUnavailable;
		/// <summary>Can this button have an associated inventory value.</summary>
		/// <remarks>Eventually this can include things like items as well.</remarks>
		public bool HasInventoryValue { get { return BlockType != 0; } }
		public bool HasKeyBind { get { return KeyBind != 0; } }

		//if a button is a block it will have the block type, if its a tool it will have the tool type, it never has both, if its a light source it has LightSourceType
		//inheritance may have made sense here, however its added complication for small benefit right now because some places use buttons and check both etc.
		public Block.BlockType BlockType;
		public GameObjects.GameItems.LightSourceType? LightSourceType;
		public ToolType ToolType { get; set; }
		
		public bool ContainsCoords(int x, int y)
		{
			return x > X && x < X + Buttons.BUTTON_SIZE && y > Y && y < Y + Buttons.BUTTON_SIZE;
		}

		public void Render()
		{
			//render texture
			GL.BindTexture(TextureTarget.Texture2D, Texture);
			GL.PushMatrix();
			GL.Translate(X, Y, 0);
			if (Hightlight) GL.Color3(Color.Lime); //else if (HighlightUnavailable) GL.Color3(Color.Red);
			GL.CallList(Utilities.DisplayList.UiButtonId);
			//if (Hightlight || HighlightUnavailable) Utilities.GlHelper.ResetColor();
			if (Hightlight) Utilities.GlHelper.ResetColor();

			if ((!Config.CreativeMode && HasInventoryValue) || HasKeyBind) //there will be one or both numeric overlays on this button
			{
				GL.BindTexture(TextureTarget.Texture2D, Textures.TextureLoader.CharacterAtlasSmall);

				if (!Config.CreativeMode && HasInventoryValue) //render overlay inventory value on button
				{
					GL.Color3(Color.Yellow);
					var str = Game.Player.Inventory[(int)BlockType].ToString();
					GL.PushMatrix();
					GL.Translate(Buttons.BUTTON_SIZE - (str.Length * Textures.TextureLoader.DEFAULT_FONT_SMALL_WIDTH), Buttons.BUTTON_SIZE - Textures.TextureLoader.DefaultFontSmallHeight, 0);
					for (var i = 0; i < str.Length; i++) //loop because the inventory value can be more than one character; ie 10+
					{
						if (i > 0) GL.Translate(Textures.TextureLoader.DEFAULT_FONT_SMALL_WIDTH, 0, 0);
						GL.CallList(Utilities.DisplayList.SmallCharAtlasIds[str[i]]);
					}
					GL.PopMatrix();
					Utilities.GlHelper.ResetColor();
				}

				if (HasKeyBind) //render overlay keybind
				{
					GL.CallList(Utilities.DisplayList.SmallCharAtlasIds[KeyBind]);
				}
			}

			GL.PopMatrix();
		}
	}
}
