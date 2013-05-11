using System;
using System.Drawing;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.Hosts.Ui;
using Hexpoint.Blox.Hosts.World;
using OpenTK;
using OpenTK.Input;

namespace Hexpoint.Blox.Hosts.Input
{
	internal class InputHost : IHost
	{
		public InputHost()
		{
			_game = Settings.Game;
			_game.Mouse.Move += OnMouseMoveHandler;
			_game.Mouse.ButtonDown += OnMouseButtonDown;
			_game.Mouse.ButtonUp += OnMouseButtonUp;
			_game.KeyPress += OnKeyPress;
			_game.Keyboard.KeyDown += Keyboard_KeyDown;
			_game.Keyboard.KeyUp += Keyboard_KeyUp;
		}

		#region Properties
		private readonly Game _game; //provides a shortcut to game (Settings.Game could also be used instead)
		private static float _jumpVelocity; //remaining velocity on the current jump
		private Point _rightMouseDownLocation;
		private bool _autoRun;

		private static bool _isStandingOnSolidGround;
		/// <summary>Is the player standing on solid ground. Some actions, such as starting a jump, require the player to be on solid ground.</summary>
		public static bool IsStandingOnSolidGround
		{
			get { return _isStandingOnSolidGround; }
			set
			{
				_isStandingOnSolidGround = value;
				if (_isStandingOnSolidGround) Game.Player.FallVelocity = 0;
			}
		}

		private static bool _isFloating;
		/// <summary>Is the player flying or floating while swimming. Flying only allowed in creative mode. The player doesnt fall while flying.</summary>
		internal static bool IsFloating
		{
			get { return _isFloating; }
			set
			{
				_isFloating = value;
				if (Game.Player != null) Game.Player.FallVelocity = 0;
				IsJumping = false;
			}
		}

		private static bool _isJumping;
		/// <summary>Is the player jumping. Only true if the player is still in the upwards trajectory of the jump, otherwise the player is falling.</summary>
		internal static bool IsJumping
		{
			get { return _isJumping; }
			set
			{
				_isJumping = value;
				_jumpVelocity = _isJumping ? Settings.JumpSpeed : 0;
			}
		}
		#endregion

		#region Key Handling
		/// <summary>Use this method for keys that should be handled repeatedly.</summary>
		public void Update(FrameEventArgs e)
		{
			Movement.CheckPlayerEnteringOrExitingWater();

			if (IsJumping)
			{
				_jumpVelocity -= (float)e.Time;
				var jumpDist = _jumpVelocity;
				if (Math.Abs(jumpDist) > 1) jumpDist = Math.Sign(jumpDist);
				var destCoords = Game.Player.Coords;
				destCoords.Yf += jumpDist;
				if (!destCoords.IsValidPlayerLocation) //cancel jump due to collision
				{
					IsJumping = false;
				}
				else
				{
					Game.Player.Coords.Yf += jumpDist;
					NetworkClient.SendPlayerLocation(Game.Player.Coords);
					if (_jumpVelocity <= 0) IsJumping = false; //at the top of the jump arc
				}
			}
			else if (!IsFloating && !(Game.Player.EyesUnderWater && _game.Keyboard[Key.Space])) //"fall" if not floating and not under water while pressing space to ascend
			{
				if (Game.Player.EyesUnderWater)
				{
					Game.Player.FallVelocity = Constants.UNDER_WATER_FALL_VELOCITY; //constant slow under water fall speed
				}
				else if (Game.Player.FallVelocity < Constants.MAX_FALL_VELOCITY)
				{
					Game.Player.FallVelocity = Math.Min(Game.Player.FallVelocity + ((float)e.Time / 2), Constants.MAX_FALL_VELOCITY);
				}

				var destCoords = Game.Player.Coords;
				destCoords.Yf -= (Game.Player.FallVelocity + Constants.MOVE_COLLISION_BUFFER); //account for fall speed and the collision buffer

				if (destCoords.Yf >= Chunk.CHUNK_HEIGHT || !WorldData.GetBlock(ref destCoords).IsSolid) //player wont hit a solid block so let them keep falling
				{
					IsStandingOnSolidGround = false; //gm: needed in the case the player walks off a cliff without jumping and then tries to jump mid air
					Game.Player.Coords.Yf -= Game.Player.FallVelocity;
					NetworkClient.SendPlayerLocation(Game.Player.Coords);
				}
				else //player is either standing on a solid block or will now land on one
				{
					if (Game.Player.Coords.Yf - Constants.MOVE_COLLISION_BUFFER > Game.Player.Coords.Yblock) //player is landing on a solid block
					{
						Game.Player.Coords.Yf = Game.Player.Coords.Yblock + Constants.PLAYER_GROUNDED_VERTICAL_BUFFER;
						if (!Game.Player.FeetUnderWater) Sounds.Audio.PlaySound(Sounds.SoundType.PlayerLanding, Game.Player.FallVelocity * 1.5f); //plays sound louder based on the velocity at time of landing
						NetworkClient.SendPlayerLocation(Game.Player.Coords, true); //forcefully send to server so other players are always seen "grounded"
					}
					IsStandingOnSolidGround = true; //gm: cant set inside the above block only because if trying to jump while in a 2 block high tunnel it could get toggled off, this must be set after the sound so the fallVelocity hasnt been reset yet
				}
			}

			//dont process movement keys while chatting OR a control key is pressed because this can give all the keys different meanings
			if (Game.UiHost.IsChatting || _game.Keyboard[Key.ControlLeft] || _game.Keyboard[Key.ControlRight])
			{
				if (_autoRun) Movement.MovePlayer(true, false, false, false, e); //allow autorun to continue
				return;
			}

			#region Player Movement
			//player movement rules
			//1. if moving forward and back, moving forward will win (tiny performance benefit of not needing to check down or S keys when moving forward)
			//2. if strafing both directions at the same time they will cancel each other out
			//3. holding the right mouse button while turning left or right will strafe instead
			//4. autorun continues until canceled by forward or back move
			bool moveForward = _autoRun;
			bool moveBack = false;
			bool strafeRight = false;
			bool strafeLeft = false;
			
			if (_game.Keyboard[Key.Up] || _game.Keyboard[Key.W])
			{
				if (_autoRun) _autoRun = false; else moveForward = true;
			}
			else if (_game.Keyboard[Key.Down] || _game.Keyboard[Key.S])
			{
				if (_autoRun) _autoRun = false; else moveBack = true;
			}

			if (_game.Keyboard[Key.Left] || _game.Keyboard[Key.A]) { if (!_game.Mouse[MouseButton.Right]) Movement.RotateDirection(-Constants.TURN_SPEED * (float)e.Time); else strafeLeft = true; }
			if (_game.Keyboard[Key.Right] || _game.Keyboard[Key.D]) { if (!_game.Mouse[MouseButton.Right]) Movement.RotateDirection(Constants.TURN_SPEED * (float)e.Time); else strafeRight = true; }
			if (_game.Keyboard[Key.Q]) strafeLeft = true;
			if (_game.Keyboard[Key.E]) strafeRight = true;

			//only move if moving 1) forward 2) back 3) strafing one but not both directions
			if (moveForward || moveBack || (strafeLeft ^ strafeRight))
			{
				//by having only a single call to process movement it prevents all issues with double moves and speed hacks from combinations of keys, strafing, etc.
				Movement.MovePlayer(moveForward, moveBack, strafeLeft, strafeRight, e);
			}
			#endregion

			if (_game.Keyboard[Key.Space])
			{
				if (IsFloating || Game.Player.EyesUnderWater) //ascend while flying/swimming
				{
					var destCoords = Game.Player.Coords;
					var distance = (float)(Settings.MoveSpeed * e.Time);
					if (Game.Player.EyesUnderWater) distance *= 0.5f; //ascend slower under water
					destCoords.Yf += distance;
					if (destCoords.IsValidPlayerLocation)
					{
						Game.Player.Coords.Yf += distance;
						NetworkClient.SendPlayerLocation(Game.Player.Coords);
					}
				}
				else if (!IsJumping && (IsStandingOnSolidGround || Game.Player.FeetUnderWater)) //jump
				{
					IsFloating = false;
					IsStandingOnSolidGround = false;
					IsJumping = true;
				}
			}

			//field of view testing
			if (Config.CreativeMode)
			{
				if (_game.Keyboard[Key.F5]) Settings.FieldOfView -= 0.03f;
				if (_game.Keyboard[Key.F6]) Settings.FieldOfView += 0.03f;
				if (_game.Keyboard[Key.F7]) Settings.FieldOfView = Constants.DEFAULT_FIELD_OF_VIEW; //reset to default
			}

			//mwahaha
			if (_game.Keyboard[Key.R])
			{
				var v = new Vector3(Settings.Random.Next(10, 30) * (float)Math.Cos(Game.Player.Coords.Direction) * (float)Math.Cos(Game.Player.Coords.Pitch),
					Settings.Random.Next(5, 25) * (float)Math.Sin(Game.Player.Coords.Pitch) + 15,
					Settings.Random.Next(10, 30) * (float)Math.Sin(Game.Player.Coords.Direction) * (float)Math.Cos(Game.Player.Coords.Pitch));
				var c = Game.Player.Coords;
				c.Yf += 0.5f;
				new AddProjectile(ref c, ref v, Block.BlockType.Lava, true).Send();
			}
		}

		/// <summary>
		/// This event is better suited for handling text while chatting as the key presses can be transferred straight through unlike the OpenTK KeyDown and KeyUp events.
		/// This event fires for ESC and ENTER but not for Function keys, PrtScr, Home, End, PgUp, PgDn, etc.
		/// Allows key repeat by default.
		/// </summary>
		private void OnKeyPress(object sender, KeyPressEventArgs e)
		{
			if (Game.UiHost.IsChatting) { Game.UiHost.AddChatKey(e); return; } //dont process further keys while chatting
			Block.BlockType? blockType;
			LightSourceType? lightSourceType;
			if (!Buttons.IsActionKey(e.KeyChar, out blockType, out lightSourceType)) return;
			if (blockType.HasValue)
			{
				AddOrRemoveBlock(blockType.Value);
			}
			else if (lightSourceType.HasValue)
			{
				var position = BlockCursorHost.PositionAdd;
				if (WorldData.IsValidStaticItemPosition(position))
				{
					var lightSource = new LightSource(ref position, lightSourceType.Value, BlockCursorHost.SelectedFace.ToOpposite());
					new AddStaticItem(lightSource).Send();
				}
			}
		}

		/// <summary>
		/// Use this method for keys that should only execute once when a key is pressed. Ie: building a block or saving game.
		/// This event uses the OpenTK key enumeration which fires for all keys, however the enum value is only useful for determining
		/// which key was pressed and not for passing through to ui chat etc.
		/// Does not allow rey repeat by default.
		/// </summary>
		private void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
		{
			bool isAltPressed = _game.Keyboard[Key.AltLeft] || _game.Keyboard[Key.AltRight];
			bool isCtrlPressed = _game.Keyboard[Key.ControlLeft] || _game.Keyboard[Key.ControlRight];
			if (e.Key == Key.F4 && isAltPressed) _game.Close(); //always check if game should close first
			if (e.Key == Key.Enter) Game.UiHost.ToggleChat();

			if (Game.UiHost.IsChatting)
			{
				switch (e.Key)
				{
					case Key.Up:
						Game.UiHost.AddChatKeys(SlashCommands.LastSlashCommand);
						SlashCommands.LastSlashCommand = "";
						break;
					case Key.V: //paste
						if (isCtrlPressed) PasteTextFromClipboard();
						break;
				}
				return; //ignore all other characters while chatting, the OnKeyPress event will handle the ones we want to pass through to chatting
			}

			switch (e.Key)
			{
				case Key.C:
					if (isCtrlPressed) new PlayerOption(PlayerOption.OptionType.Creative, BitConverter.GetBytes(Config.CreativeMode ? 0 : 1)).Send();
					break;
				case Key.S:
					if (isCtrlPressed) new PlayerOption(PlayerOption.OptionType.Speed, BitConverter.GetBytes(Math.Abs(Settings.MoveSpeed - Constants.MOVE_SPEED_DEFAULT) < 0.1 ? 5 : 1)).Send();
					break;
				case Key.T: //bouncing bomb
				case Key.Y: //sticky bomb
					var v = new Vector3(Settings.Random.Next(32, 40) * (float)Math.Cos(Game.Player.Coords.Direction) * (float)Math.Cos(Game.Player.Coords.Pitch),
						Settings.Random.Next(25, 35) * (float)Math.Sin(Game.Player.Coords.Pitch) + 10,
						Settings.Random.Next(32, 40) * (float)Math.Sin(Game.Player.Coords.Direction) * (float)Math.Cos(Game.Player.Coords.Pitch));
					var c = Game.Player.Coords;
					c.Yf += 0.5f;
					new AddProjectile(ref c, ref v, Block.BlockType.Lava, e.Key == Key.T).Send();
					break;
				case Key.V:
					if (isCtrlPressed)
					{
						Game.UiHost.ToggleChat();
						PasteTextFromClipboard();
					}
					break;
				case Key.Z:
					if (isAltPressed) Settings.UiDisabled = !Settings.UiDisabled;
					break;
				case Key.Slash:
					Game.UiHost.OpenSlashCommand();
					break;
				case Key.Space:
					//cancel flying (middle mouse button can now be used instead, leaving this for old habits)
					if (Config.CreativeMode && isCtrlPressed) IsFloating = false;
					break;
				case Key.Tilde:
					AddOrRemoveBlock(Block.BlockType.Air);
					break;
				case Key.F1:
					SlashCommands.DisplayCommandHelp();
					break;
				case Key.F3:
					Settings.UiDebugDisabled = !Settings.UiDebugDisabled;
					break;
				case Key.F10:
					//toggle full screen (provides a pretty decent fps increase, may want to consider having slower comps/laptops default to full screen)
					_game.WindowState = _game.WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
					break;
				case Key.F11:
					//toggle maximized
					_game.WindowState = _game.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
					break;
				case Key.F12:
					Utilities.Misc.CaptureScreenshot(_game);
					Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.SlashResult, "Screenshot saved."));
					break;
			}
		}

		/// <summary>Use this method for keys that should only execute once when a key is released. Behaviour is similar to the KeyDown event.</summary>
		private void Keyboard_KeyUp(object sender, KeyboardKeyEventArgs e)
		{
			//System.Diagnostics.Debug.WriteLine("key up: {0}", e.Key);
		}

		/// <summary>Paste text to chat if the clipboard contains text and is less then the maximum length.</summary>
		private void PasteTextFromClipboard()
		{
			if (!System.Windows.Forms.Clipboard.ContainsText()) return;
			var text = System.Windows.Forms.Clipboard.GetText();
			if (text.Length <= 80) Game.UiHost.AddChatKeys(text); else Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Error, "Max allowed clipboard length is 80 chars."));
		}
		#endregion

		#region Mouse Handling
		private void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
		{
			_rightMouseDownLocation = System.Windows.Forms.Cursor.Position;
			switch (e.Button)
			{
				case MouseButton.Left:
					Buttons.LeftMouseDown(e.X, e.Y);
					break;
				case MouseButton.Right:
					System.Windows.Forms.Cursor.Hide(); //hide mouse cursor while right mouse button is down
					break;
				case MouseButton.Middle:
					if (Config.CreativeMode) IsFloating = !IsFloating;
					break;
				case MouseButton.Button1:
					_autoRun = !_autoRun;
					break;
			}
		}

		private static void OnMouseButtonUp(object sender, MouseButtonEventArgs e)
		{
			switch (e.Button)
			{
				case MouseButton.Left:
					Buttons.LeftMouseUp(e.X, e.Y);
					break;
				case MouseButton.Right:
					System.Windows.Forms.Cursor.Show(); //show mouse cursor while right mouse button is up
					break;
			}
		}

		public void OnMouseMoveHandler(object sender, MouseEventArgs e)
		{
			if (_game.Mouse[MouseButton.Right] && _rightMouseDownLocation != System.Windows.Forms.Cursor.Position)
			{
				if(_rightMouseDownLocation.X != System.Windows.Forms.Cursor.Position.X)
				{
					//mouse x changed, rotate direction
					Movement.RotateDirection(MathHelper.DegreesToRadians((System.Windows.Forms.Cursor.Position.X - _rightMouseDownLocation.X) / 4f));
				}
				if (_rightMouseDownLocation.Y != System.Windows.Forms.Cursor.Position.Y)
				{
					//mouse y changed, rotate pitch
					Movement.RotatePitch(MathHelper.DegreesToRadians((System.Windows.Forms.Cursor.Position.Y - _rightMouseDownLocation.Y) / 3f * (Config.InvertMouse ? 1 : -1)));
				}
				System.Windows.Forms.Cursor.Position = _rightMouseDownLocation;
			}
		}
		#endregion

		public void AddOrRemoveBlock(Block.BlockType blockType)
		{
			if (!BlockCursorHost.Position.IsValidBlockLocation)
			{
				Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Error, "Invalid block location."));
				return;
			}

			var blockAtCursor = BlockCursorHost.Position.GetBlock();
			if (!Config.CreativeMode && blockAtCursor.Type == Block.BlockType.Water)
			{
				if (!blockAtCursor.IsDirty)
				{
					Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Error, "Only player-made water may be removed."));
					return;
				}
				if (blockType == Block.BlockType.Air)
				{
					Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Error, "Water must be removed by building on it."));
					return;
				}
			}
			NetworkClient.SendAddOrRemoveBlock(blockType == Block.BlockType.Air || (blockType != Block.BlockType.Water && blockAtCursor.Type == Block.BlockType.Water) ? BlockCursorHost.Position : BlockCursorHost.PositionAdd, blockType);
		}

		public void Render(FrameEventArgs e)
		{
			//nothing to render for input
		}

		public void Resize(EventArgs e)
		{

		}

		public void Dispose()
		{
			
		}

		public bool Enabled { get; set; }
	}
}
