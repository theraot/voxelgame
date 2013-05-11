using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.Hosts.Input;
using Hexpoint.Blox.Textures;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Hosts.Ui
{
	internal class UiHost : IHost
	{
		#region Constructors
		internal UiHost()
		{
			Buttons.Load();
		}
		#endregion

		#region Properties
		private enum TextLocation : byte { TopLeft, TopRight, BottomLeft }
		private const int HORIZONTAL_MARGIN = 5; //margin for text from the sides of the window horizontally
		private int _linesTopLeft;
		private int _linesTopRight;
		private int _linesBottomLeft;

		private readonly Queue<ChatMessage> _chatMessages = new Queue<ChatMessage>(); //bm: i get about a 3% framerate hit if i change this to concurrentqueue
		public bool IsChatting { get; private set; }
		public string CurrentChatText = "";
		#endregion

		public void Update(FrameEventArgs e)
		{

		}

		public void Render(FrameEventArgs e)
		{
			if (Settings.UiDisabled) return;

			//load identity matrices
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();

			GL.PushAttrib(AttribMask.EnableBit);
			GL.Enable(EnableCap.Blend);
			GL.Disable(EnableCap.DepthTest); //disable depth testing while drawing ui because clipping/collision issues would otherwise sometimes be able to overwrite the ui (might also be a tiny performance benefit)
			GL.Ortho(0, Settings.Game.Width, Settings.Game.Height, 0, -1, 1);

			_linesTopLeft = 0;
			_linesTopRight = 0;
			_linesBottomLeft = 0;
			RenderCompass();
			RenderText();
			Buttons.Render();

			#region debug: display the full texture atlas
			//GL.BindTexture(TextureTarget.Texture2D, TextureLoader.CharacterAtlasSmall);
			//GL.Begin(BeginMode.Quads);
			//GL.TexCoord2(1, 0); GL.Vertex2(Constants.TOTAL_ASCII_CHARS * 11, 350);
			//GL.TexCoord2(0, 0); GL.Vertex2(0, 350);
			//GL.TexCoord2(0, 1); GL.Vertex2(0, 365);
			//GL.TexCoord2(1, 1); GL.Vertex2(Constants.TOTAL_ASCII_CHARS * 11, 365);
			//GL.End();
			#endregion

			GL.PopAttrib();

			//restore original matrices
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref Game.Projection);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref Game.ModelView);
		}

		private static void RenderCompass()
		{
			const int MARGIN_RIGHT = 20;
			const int MARGIN_TOP = 40;
			GL.PushMatrix();
			GL.BindTexture(TextureTarget.Texture2D, TextureLoader.GetUiTexture(UiTextureType.CompassArrow));
			GL.Translate(Settings.Game.Width - MARGIN_RIGHT, MARGIN_TOP, 0);
			GL.Rotate(MathHelper.RadiansToDegrees(Game.Player.Coords.Direction) + 90, Vector3.UnitZ);
			GL.CallList(Utilities.DisplayList.CompassId);
			GL.PopMatrix();
		}

		private void RenderText()
		{
			GL.BindTexture(TextureTarget.Texture2D, TextureLoader.CharacterAtlasSmall);
			GL.Color4(Color.Yellow);
			WriteString(TextLocation.TopRight, Settings.VersionDisplay); //useful to have version on ui for screenshots / vid captures
			WriteString(TextLocation.TopRight, string.Format("{0}", Game.Player.Coords.DirectionFacing()));

			if (!Settings.UiDebugDisabled)
			{
				WriteString(TextLocation.TopLeft, Utilities.DisplayList.TextF3ToHideId);
				WriteString(TextLocation.TopLeft, Utilities.DisplayList.TextPlayerId);
				WriteString(TextLocation.TopLeft, string.Format("X:{0}", Game.Player.Coords.Xblock));
				WriteString(TextLocation.TopLeft, string.Format("Y:{0}", Game.Player.Coords.Yblock));
				WriteString(TextLocation.TopLeft, string.Format("Z:{0}", Game.Player.Coords.Zblock));
				WriteString(TextLocation.TopLeft, string.Format("D:{0:f0}", MathHelper.RadiansToDegrees(Game.Player.Coords.Direction))); //the format rounds the value so its less deceiving then truncating it like before
				WriteString(TextLocation.TopLeft, string.Format("P:{0:f0}", MathHelper.RadiansToDegrees(Game.Player.Coords.Pitch))); //the format rounds the value so its less deceiving then truncating it like before

				WriteBlankLine(TextLocation.TopLeft);
				WriteString(TextLocation.TopLeft, Utilities.DisplayList.TextCursorId);
				WriteString(TextLocation.TopLeft, string.Format("X:{0}", BlockCursorHost.Position.X));
				WriteString(TextLocation.TopLeft, string.Format("Y:{0}", BlockCursorHost.Position.Y));
				WriteString(TextLocation.TopLeft, string.Format("Z:{0}", BlockCursorHost.Position.Z));
				WriteString(TextLocation.TopLeft, string.Format("L:{0}", BlockCursorHost.PositionAdd.LightStrength));
				WriteString(TextLocation.TopLeft, string.Format("F:{0}", BlockCursorHost.Position.IsValidBlockLocation ? BlockCursorHost.SelectedFace.ToString() : "NA"));
				WriteString(TextLocation.TopLeft, string.Format("T:{0}", BlockCursorHost.Position.IsValidBlockLocation ? BlockCursorHost.Position.GetBlock().Type.ToString() : "NA"));

				WriteBlankLine(TextLocation.TopLeft);
				WriteString(TextLocation.TopLeft, Utilities.DisplayList.TextPerformanceId);
				WriteString(TextLocation.TopLeft, string.Format("FPS:{0}", Game.PerformanceHost.Fps));
				WriteString(TextLocation.TopLeft, string.Format("MEM:{0} MB", Game.PerformanceHost.Memory));
				WriteString(TextLocation.TopLeft, string.Format("Threads:{0}/{1}", Game.PerformanceHost.ActiveThreads, Game.PerformanceHost.TotalThreads));
				WriteString(TextLocation.TopLeft, string.Format("Chunks:{0}/{1}", Game.PerformanceHost.ChunksRendered, Game.PerformanceHost.ChunksInMemory));
			}

			lock (_chatMessages)
			{
				foreach (var chatMessage in _chatMessages)
				{
					GL.Color3(chatMessage.Color);
					WriteString(TextLocation.BottomLeft, chatMessage.Message);
				}
			}
			if (IsChatting)
			{
				GL.Color4(Color.LightCyan);
				WriteString(TextLocation.BottomLeft, string.Format("{0}{1}", CurrentChatText, Game.PerformanceHost.IsAlternateSecond ? "" : "_")); //write current text & flash the input cursor
			}
			Utilities.GlHelper.ResetColor();
		}

		private void WriteString(TextLocation location, string text)
		{
			GL.PushMatrix();
			GL.Translate(location == TextLocation.TopRight ? Settings.Game.Width - HORIZONTAL_MARGIN - (text.Length * TextureLoader.DEFAULT_FONT_SMALL_WIDTH) : HORIZONTAL_MARGIN, GetNextCursorY(location), 0);
			for (int i = 0; i < text.Length; i++)
			{
				if (i > 0) GL.Translate(TextureLoader.DEFAULT_FONT_SMALL_WIDTH, 0, 0); //note: dont need to translate to first char, reduces one GL call per text line
				if (text[i] == ' ') continue; //gm: no need to render spaces
				GL.CallList(Utilities.DisplayList.SmallCharAtlasIds[text[i]]);
			}
			GL.PopMatrix();
		}

		private void WriteString(TextLocation location, int textDisplayListId)
		{
			Debug.Assert(location != TextLocation.TopRight, "Rendering text display lists cant work on the right side yet as we dont know how long the text is.");
			GL.PushMatrix();
			GL.Translate(HORIZONTAL_MARGIN, GetNextCursorY(location), 0);
			GL.CallList(textDisplayListId);
			GL.PopMatrix();
		}

		private void WriteBlankLine(TextLocation location)
		{
			switch (location)
			{
				case TextLocation.TopLeft:
					_linesTopLeft++;
					return;
				case TextLocation.TopRight:
					_linesTopRight++;
					return;
				case TextLocation.BottomLeft:
					_linesBottomLeft++;
					return;
				default: throw new Exception("Unknown TextLocation: " + location);
			}
		}

		private int GetNextCursorY(TextLocation location)
		{
			int y;
			switch (location)
			{
				case TextLocation.TopLeft:
					y = _linesTopLeft * TextureLoader.DefaultFontSmallHeight;
					_linesTopLeft++;
					break;
				case TextLocation.TopRight:
					y = _linesTopRight * TextureLoader.DefaultFontSmallHeight;
					_linesTopRight++;
					break;
				case TextLocation.BottomLeft:
					y = Settings.Game.Height - Buttons.BUTTON_SIZE - ((_chatMessages.Count - _linesBottomLeft + 1) * TextureLoader.DefaultFontSmallHeight); //show above action bar
					_linesBottomLeft++;
					break;
				default: throw new Exception("Unknown TextLocation: " + location);
			}
			return y;
		}

		public void Resize(EventArgs e)
		{
			Buttons.Load(); //todo: this just recreates all the buttons to fix the positions. make it better.
		}

		public void AddChatMessage(ChatMessage chatMessage)
		{
			// ReSharper disable RedundantStringFormatCall (gm: need the string.format when using only a single string param or it matches the category signature instead)
			Debug.WriteLine(string.Format("[Client] {0}", chatMessage.Message));
			// ReSharper restore RedundantStringFormatCall
			lock (_chatMessages)
			{
				_chatMessages.Enqueue(chatMessage);
				if (_chatMessages.Count > 10) _chatMessages.Dequeue();
			}
		}

		public void ClearChatMessages()
		{
			_chatMessages.Clear();
		}

		public void ToggleChat()
		{
			IsChatting = !IsChatting;
			if (IsChatting || CurrentChatText.Length <= 0) return;
			if (CurrentChatText.StartsWith("/")) //this is a client command that shouldnt be sent to the server
			{
				SlashCommands.ProcessSlashCommand(CurrentChatText);
			}
			else //send this text to the server
			{
				new ChatMsg(Game.Player.Id, CurrentChatText).Send();
			}
			CurrentChatText = "";
		}

		public void OpenSlashCommand()
		{
			if (!IsChatting)
			{
				IsChatting = true;
				CurrentChatText = "/";
			}
		}

		public void AddChatKey(KeyPressEventArgs e)
		{
			var key = (int)e.KeyChar;
			if (key > Constants.HIGHEST_ASCII_CHAR || (key < Constants.LOWEST_ASCII_CHAR && key != 8)) return; //ignore all strange characters except for 8=backspace
			switch (key)
			{
				case 47: //forward slash
					if (CurrentChatText == "/") return; //dont allow more slashes when theres only one slash for a slash command (key repeat on some comps seems to make them too quick)
					CurrentChatText += e.KeyChar;
					break;
				case 8: //backspace
					if (CurrentChatText.Length == 0) return;
					CurrentChatText = CurrentChatText.Substring(0, CurrentChatText.Length - 1);
					break;
				default:
					CurrentChatText += e.KeyChar;
					break;
			}
		}

		public void AddChatKeys(string message)
		{
			foreach (var c in message)
			{
				if (c < Constants.LOWEST_ASCII_CHAR || c > Constants.HIGHEST_ASCII_CHAR) continue; //ignore all strange characters
				CurrentChatText += c;
			}
		}

		public void Dispose()
		{

		}

		public bool Enabled { get; set; }
	}
}
