using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Textures;

namespace Hexpoint.Blox.Hosts.Ui
{
	internal static class Buttons
	{
		#region Constructors
		static Buttons()
		{
			FastToolTimer = new Timer(200);
			FastToolTimer.Elapsed += _fastToolTimer_Elapsed;
		}
		#endregion

		#region Properties
		public const int NUMBER_OF_ACTION_BAR_BUTTONS = 10;
		public const int BUTTON_SIZE = 50;
		public const int GRID_BUTTONS_PER_ROW = 10;

		private static Button[] _actionButtons;
		private static List<Button> _blockPickerGridButtons;
		private static Button[] _toolButtons;

		private static bool _blockPickerGridVisible;
		/// <summary>This is the grid of selectable block types that appears in the middle of the screen.</summary>
		public static bool BlockPickerGridVisible
		{
			get { return _blockPickerGridVisible; }
			set
			{
				if (!value && _currentActionButton != null) _currentActionButton.Hightlight = false;
				_currentActionButton = null;
				_blockPickerGridVisible = value;
			}
		}

		/// <summary>Track the currently selected Action (bottom bar) button. Null if none selected.</summary>
		private static Button _currentActionButton;

		/// <summary>Track the currently selected Tool (right bar) button. Null if none selected.</summary>
		private static Button _currentToolButton;

		/// <summary>Track the previously selected Tool (right bar) button. Null if not applicable. Used to return to previous tool in some scenarios.</summary>
		private static Button _previousToolButton;
		
		public static ToolType CurrentTool { get { return _currentToolButton.ToolType; } }

		/// <summary>Select a new tool or reset to the default.</summary>
		internal static void SelectTool(ToolType toolType)
		{
			_currentToolButton.Hightlight = false;
			_previousToolButton = _currentToolButton;
			_currentToolButton = _toolButtons[(int)toolType];
			_currentToolButton.Hightlight = true;
		}

		/// <summary>Used by the fast add and fast remove tools. Started only when those tools are selected.</summary>
		private static readonly Timer FastToolTimer;
		#endregion

		public static void Load()
		{
			//load action buttons
			_actionButtons = new Button[NUMBER_OF_ACTION_BAR_BUTTONS];

			for (var i = 0; i <= NUMBER_OF_ACTION_BAR_BUTTONS - 1; i++)
			{
				var button = new Button(ButtonType.Action, Settings.Game.Width / 2 - (NUMBER_OF_ACTION_BAR_BUTTONS / 2 * BUTTON_SIZE) + (i * BUTTON_SIZE), Settings.Game.Height - BUTTON_SIZE) { KeyBind = Convert.ToChar((i == 9 ? 0 : i + 1).ToString()) };
				switch (i + 1)
				{
					case 1:
						button.Texture = TextureLoader.GetUiTexture(UiTextureType.Shovel);
						button.BlockType = Block.BlockType.Air;
						break;
					case 2:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.Sand);
						button.BlockType = Block.BlockType.Sand;
						break;
					case 3:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.Bricks);
						button.BlockType = Block.BlockType.Bricks;
						break;
					case 4:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.Cobble);
						button.BlockType = Block.BlockType.Cobble;
						break;
					case 5:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.SteelPlate);
						button.BlockType = Block.BlockType.SteelPlate;
						break;
					case 6:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.TreeTrunk);
						button.BlockType = Block.BlockType.Tree;
						break;
					case 7:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.WoodTile1);
						button.BlockType = Block.BlockType.WoodTile1;
						break;
					case 8:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.Grass);
						button.BlockType = Block.BlockType.Grass;
						break;
					case 9:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.Dirt);
						button.BlockType = Block.BlockType.Dirt;
						break;
					case 10:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.Gravel);
						button.BlockType = Block.BlockType.Gravel;
						break;
				}
				_actionButtons[i] = button;
			}

			//load block type picker grid buttons
			_blockPickerGridButtons = new List<Button>(Enum.GetValues(typeof(Block.BlockType)).Length + Enum.GetValues(typeof(LightSourceType)).Length);
			var buttonColumn = 1;
			var buttonY = Settings.Game.Height / 2 - (_blockPickerGridButtons.Capacity / GRID_BUTTONS_PER_ROW * BUTTON_SIZE / 2);
			foreach (Block.BlockType blockType in Enum.GetValues(typeof(Block.BlockType)))
			{
				AddButtonToPickerGrid(ref buttonColumn, ref buttonY, blockType.ToString(), blockType == Block.BlockType.Air ? TextureLoader.GetUiTexture(UiTextureType.Shovel) : TextureLoader.GetBlockTexture(Block.FaceTexture(blockType, Face.Top)), blockType);
			}
			foreach (LightSourceType lightSourceType in Enum.GetValues(typeof(LightSourceType)))
			{
				AddButtonToPickerGrid(ref buttonColumn, ref buttonY, lightSourceType.ToString(), TextureLoader.GetItemTexture(ItemTextureType.Lantern), lightSourceType: lightSourceType);
			}

			//load tool buttons
			_toolButtons = new Button[Enum.GetValues(typeof(ToolType)).Length];
			for (var i = 0; i < _toolButtons.Length; i++)
			{
				var button = new Button(ButtonType.Tool, Settings.Game.Width - BUTTON_SIZE, Settings.Game.Height / 2 - _toolButtons.Length * BUTTON_SIZE / 2 + i * BUTTON_SIZE) { ToolType = (ToolType)i };
				switch((ToolType)i)
				{
					case ToolType.Default:
						button.Texture = TextureLoader.GetUiTexture(UiTextureType.ToolDefault);
						break;
 					case ToolType.ToolBlockType:
						button.Texture = TextureLoader.GetBlockTexture(BlockTextureType.Oil);
						button.BlockType = Block.BlockType.Oil;
						break;
					case ToolType.Cuboid:
						button.Texture = TextureLoader.GetUiTexture(UiTextureType.ToolCuboid);
						break;
					case ToolType.FastBuild:
						button.Texture = TextureLoader.GetUiTexture(UiTextureType.ToolFastBuild);
						break;
					case ToolType.FastDestroy:
						button.Texture = TextureLoader.GetUiTexture(UiTextureType.ToolFastDestroy);
						break;
					case ToolType.Tree:
						button.Texture = TextureLoader.GetUiTexture(UiTextureType.ToolTree);
						break;
					case ToolType.Tower:
						button.Texture = TextureLoader.GetUiTexture(UiTextureType.Tower);
						break;
					case ToolType.SmallKeep:
						button.Texture = TextureLoader.GetUiTexture(UiTextureType.SmallKeep);
						break;
					case ToolType.LargeKeep:
						button.Texture = TextureLoader.GetUiTexture(UiTextureType.LargeKeep);
						break;
				}
				_toolButtons[i] = button;
			}

			_currentToolButton = _currentToolButton == null ? _toolButtons[0] : _toolButtons[(int)CurrentTool]; //allows retention of selected tool when reloading, for example when the window is resized
			_currentToolButton.Hightlight = true;
		}

		private static void AddButtonToPickerGrid(ref int col, ref int y, string name, int texture, Block.BlockType blockType = 0, LightSourceType? lightSourceType = null)
		{
			if (name.StartsWith("Placeholder")) return;
			Debug.Assert(!(blockType != 0 && lightSourceType.HasValue), "Cannot have block and light source types at the same time.");
			if (col % GRID_BUTTONS_PER_ROW == 0)
			{
				col = 1;
				y += BUTTON_SIZE;
			}
			var button = new Button(ButtonType.GridPicker, Settings.Game.Width / 2 - (GRID_BUTTONS_PER_ROW / 2 * BUTTON_SIZE) + (col * BUTTON_SIZE), y, texture)
				{
					BlockType = blockType,
					LightSourceType = lightSourceType
				};
			_blockPickerGridButtons.Add(button);
			col++;
		}

		/// <summary>Render all visible buttons.</summary>
		public static void Render()
		{
			foreach (var button in _actionButtons) button.Render();

			if (BlockPickerGridVisible)
			{
				foreach (var button in _blockPickerGridButtons) button.Render();
			}

			if (Config.CreativeMode) foreach (var button in _toolButtons) button.Render();
		}

		/// <summary>Check if key pressed is bound to an action button.</summary>
		public static bool IsActionKey(char keyBind, out Block.BlockType? blockType, out LightSourceType? lightSourceType)
		{
			foreach (var actionButton in _actionButtons.Where(actionButton => actionButton.KeyBind == keyBind))
			{
				if (actionButton.LightSourceType.HasValue)
				{
					lightSourceType = actionButton.LightSourceType.Value;
					blockType = null;
				}
				else
				{
					blockType = actionButton.BlockType;
					lightSourceType = null;
				}
				return true;
			}
			blockType = null;
			lightSourceType = null;
			return false;
		}

		/// <summary>Tracks left mouse down position for use with building cuboids.</summary>
		private static Position _leftMouseDownPosition;

		public static void LeftMouseDown(int x, int y)
		{
			foreach (var button in _actionButtons)
			{
				if (button.ContainsCoords(x, y)) //clicked action bar button
				{
					if (BlockPickerGridVisible) //picker already open
					{
						_currentActionButton.Hightlight = false; //unhighlight current action button
						if(button == _currentActionButton)
						{
							//reclicked currently selected button, close picker
							BlockPickerGridVisible = false;
							return;
						}
					}
					BlockPickerGridVisible = true; //open block picker
					button.Hightlight = true; //highlight selected button
					_currentActionButton = button; //store the button that was clicked so the block type can be set once picked
					_cancelMouseUp = true; //dont run mouseup for this click to prevent accidentally using current tool "through" the button that was clicked
					return;
				}
			}

			if (BlockPickerGridVisible)
			{
				foreach (var button in _blockPickerGridButtons)
				{
					if (button.ContainsCoords(x, y)) //block picked from grid
					{
						_currentActionButton.Texture = button.Texture;
						_currentActionButton.BlockType = button.BlockType;
						_currentActionButton.LightSourceType = button.LightSourceType;

						if (_currentActionButton.ToolType == ToolType.ToolBlockType)
						{
							//selected block type for right bar block type; re-select previous tool
							_currentToolButton = _previousToolButton;
							_currentToolButton.Hightlight = true;
							_previousToolButton = null;
						}

						BlockPickerGridVisible = false;
						_cancelMouseUp = true;
						_currentActionButton = null;
						return;
					}
				}
			}

			if (Config.CreativeMode)
			{
				foreach (var button in _toolButtons)
				{
					if (button.ContainsCoords(x, y)) //tool button clicked
					{
						SelectTool(button.ToolType);
						_cancelMouseUp = true;

						if (button.ToolType == ToolType.ToolBlockType)
						{
							BlockPickerGridVisible = !BlockPickerGridVisible; //open/close block picker
							_currentActionButton = button; //store the ToolBlockType button as the current action button so the block type can be set once picked
						}
						else //close the block picker grid in case it was open for the ToolBlockType tool
						{
							BlockPickerGridVisible = false;
						}

						return;
					}
				}

				switch (CurrentTool)
				{
					case ToolType.Cuboid:
						_leftMouseDownPosition = BlockCursorHost.PositionAdd;
						break;
					case ToolType.FastBuild:
					case ToolType.FastDestroy:
						//perform one action as soon as clicked then start timer to continue if mouse held down
						_fastToolTimer_Elapsed(null, null);
						FastToolTimer.Start();
						break;
				}
			}
		}

		/// <summary>When selecting a button cancel the mouse up event to prevent unintended results.</summary>
		private static bool _cancelMouseUp;

		public static void LeftMouseUp(int x, int y)
		{
			if(_cancelMouseUp)
			{
				_cancelMouseUp = false;
				return;
			}

			if (Config.CreativeMode)
			{
				FastToolTimer.Stop();
				_prevFastPositionSent = new Position(); //reset the prev position sent, so the fast build or destroy tools can subsequently be used on the same position again

				switch (CurrentTool)
				{
					case ToolType.Cuboid:
						if (BlockCursorHost.PositionAdd.IsValidBlockLocation && _leftMouseDownPosition.IsValidBlockLocation && BlockCursorHost.PositionAdd.GetDistanceExact(ref _leftMouseDownPosition) > 3)
						{
							var leftMouseUpPosition = _toolButtons[(int)ToolType.ToolBlockType].BlockType == Block.BlockType.Air ? BlockCursorHost.Position : BlockCursorHost.PositionAdd;
							if (!leftMouseUpPosition.IsValidBlockLocation) return;
							new AddCuboid(_leftMouseDownPosition, leftMouseUpPosition, _toolButtons[(int)ToolType.ToolBlockType].BlockType).Send();
						}
						return;
					case ToolType.Tree:
						new AddStructure(BlockCursorHost.PositionAdd, StructureType.Tree, Game.Player.Coords.DirectionFacing().ToOpposite()).Send();
						return;
					case ToolType.Tower:
						new AddStructure(BlockCursorHost.PositionAdd, StructureType.Tower, Game.Player.Coords.DirectionFacing().ToOpposite()).Send();
						return;
					case ToolType.SmallKeep:
						new AddStructure(BlockCursorHost.PositionAdd, StructureType.SmallKeep, Game.Player.Coords.DirectionFacing().ToOpposite()).Send();
						return;
					case ToolType.LargeKeep:
						new AddStructure(BlockCursorHost.PositionAdd, StructureType.LargeKeep, Game.Player.Coords.DirectionFacing().ToOpposite()).Send();
						return;
				}
			}
		}

		private static Position _prevFastPositionSent;
		private static void _fastToolTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (CurrentTool == ToolType.FastDestroy && !BlockCursorHost.Position.Equals(_prevFastPositionSent))
			{
				_prevFastPositionSent = BlockCursorHost.Position;
				Game.InputHost.AddOrRemoveBlock(Block.BlockType.Air);
			}
			else if (CurrentTool == ToolType.FastBuild && !BlockCursorHost.PositionAdd.Equals(_prevFastPositionSent))
			{
				_prevFastPositionSent = BlockCursorHost.PositionAdd;
				var buttonToolBlockType = _toolButtons[(int)ToolType.ToolBlockType];
				if (buttonToolBlockType.BlockType > 0) //only add a block if a non air block type is selected
				{
					Game.InputHost.AddOrRemoveBlock(buttonToolBlockType.BlockType);
				}
				else if (buttonToolBlockType.LightSourceType.HasValue)
				{
					var position = BlockCursorHost.PositionAdd;
					if (WorldData.IsValidStaticItemPosition(position))
					{
						var lightSource = new LightSource(ref position, buttonToolBlockType.LightSourceType.Value, BlockCursorHost.SelectedFace.ToOpposite());
						new AddStaticItem(lightSource).Send();
					}
				}
			}
		}
	}
}
