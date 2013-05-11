using System;

namespace Hexpoint.Blox
{
	/// <summary>
	/// Use Settings class for values that can be altered but not saved.
	/// Use Config class for values that can be altered and saved.
	/// Use Constants class for values that cannot be altered.
	/// </summary>
	internal static class Constants
	{
		#region Game
		public const byte UPDATES_PER_SECOND = 60;
		/// <summary>Realtime amount for each game update if the game was able to run exactly on time.</summary>
		public const float UPDATE_TIME_REALTIME = 1f / UPDATES_PER_SECOND;
		/// <summary>
		/// Acceptable amount of time allowed to let a game update proceed. If it takes more than the acceptable time for an update just return;
		/// otherwise this can lead to all kinds of strange behavior. Can happen when machine gets locked up for any reason, dragging the window around is one example,
		/// but is especially evident while debugging at a breakpoint, things like movement and physics are based on the update time and a breakpoint will cause the time
		/// to become massively inflated so skipping this one update will get everything back to normal when resuming from a breakpoint.
		/// The also prevents some movement hacks a client could easily take advantage of.
		/// </summary>
		public const float UPDATE_TIME_ACCEPTABLE = UPDATE_TIME_REALTIME * 10f; //gm: was using 1.5 at first, but thats too restrictive and could sometimes make the game choppy

		/// <summary>Game now starts maximized, so this default will be the size if the user restores down. Resize also prevents game window from getting any smaller than this.</summary>
		/// <remarks>Dont go any larger as it gets too big for small laptop screens.</remarks>
		public const int DEFAULT_GAME_WIDTH = 1024;
		/// <summary>Game now starts maximized, so this default will be the size if the user restores down. Resize also prevents game window from getting any smaller than this.</summary>
		/// <remarks>
		/// Dont go any larger as it gets too big for small laptop screens.
		/// As of Apr 2012 the widest used resolution on the internet is 1366 x 768, so this value definitely should be always less than 768.
		/// 1024x576 is 16x9 PAL, 1280x720 is 16x9 720p
		/// </remarks>
		public const int DEFAULT_GAME_HEIGHT = 576;

		public const float DEFAULT_FIELD_OF_VIEW = 0.85f; //non distorted Pi/4 is 0.785

		public const float BLOCK_SIZE = 1;
		public const float HALF_BLOCK_SIZE = BLOCK_SIZE / 2;
		public const float QUARTER_BLOCK_SIZE = BLOCK_SIZE / 4;

		public const string WORLD_FILE_EXTENSION = ".vgmap";
		public const string URL = "http://www.voxelgame.com";
		#endregion

		#region Player
		/// <summary>Default movement speed.</summary>
		public const float MOVE_SPEED_DEFAULT = 4.2f;
		/// <summary>Movement collision buffer distance.</summary>
		public const float MOVE_COLLISION_BUFFER = 0.3f;
		/// <summary>Player turn speed in radians.</summary>
		public const float TURN_SPEED = 2.5f;
		/// <summary>Maximum velocity while falling.</summary>
		/// <remarks>gm: even though it might be nice to increase this a bit more, it could cause problems for collision</remarks>
		public const float MAX_FALL_VELOCITY = 0.7f;
		/// <summary>Player falls at a slower constant speed while under water.</summary>
		public const float UNDER_WATER_FALL_VELOCITY = 0.025f;
		/// <summary>Initial velocity when starting a jump.</summary>
		/// <remarks>Needs to not allow jumping vertically high enough to clear 2 stacked blocks.</remarks>
		public const float INITIAL_JUMP_VELOCITY = 0.26f;

		/// <summary>Amount to leave players off the ground when standing on a solid block. Also gets implicitly used in collision detection and eye level.</summary>
		public const float PLAYER_GROUNDED_VERTICAL_BUFFER = 0.01f;
		/// <summary>Player height. Block size * 2 minus movement collision headroom.</summary>
		public const float PLAYER_HEIGHT = (BLOCK_SIZE * 2) - PLAYER_HEADROOM;
		/// <summary>Allow some headroom for player while flying or jumping in order to smoothly fit into tight spaces.</summary>
		public const float PLAYER_HEADROOM = 0.15f;
		/// <summary>Players eye level up from their feet.</summary>
		public const float PLAYER_EYE_LEVEL = 1.6f;

		public const byte MAXIMUM_DISTANCE_TO_VIEW_NAMEPLATES = 90;
		#endregion

		#region Math
		public const float PI_OVER_6 = (float)Math.PI / 6;
		public const float PI_OVER_12 = (float)Math.PI / 12;
		#endregion

		#region Physics
		public const float GRAVITY = -9.81f;
		public const float ITEM_HOVER_DIST = 0.05f;
		#endregion

		#region Chars
		/// <summary>Lowest ASCII char we allow to be entered in chat and displayed. Note: 32 is a space, 33 is '!', 8 is backspace.</summary>
		internal const int LOWEST_ASCII_CHAR = 32;
		
		/// <summary>Highest ASCII char we allow to be entered in chat and displayed. Note: 126 is '~', 127 is delete and everything > 127 is the extended ASCII table.</summary>
		internal const int HIGHEST_ASCII_CHAR = 126;
		
		internal const int TOTAL_ASCII_CHARS = HIGHEST_ASCII_CHAR - LOWEST_ASCII_CHAR + 1;

		internal const double CHAR_ATLAS_RATIO = 1d / TOTAL_ASCII_CHARS;
		#endregion
	}
}