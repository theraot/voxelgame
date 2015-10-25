using Hexpoint.Blox.Hosts;
using Hexpoint.Blox.Hosts.Input;
using Hexpoint.Blox.Hosts.Ui;
using Hexpoint.Blox.Hosts.World;
using System;

namespace Hexpoint.Blox
{
	public enum ModeType : byte { SinglePlayer, StandaloneServer, JoinServer }

	public enum ViewDistance : byte { Tiny, Low, Standard, High, Extreme }

	public static class Configuration
	{
		private static bool _creativeMode;

		private static ModeType _mode;

		private static ViewDistance _viewDistance;

		/// <summary>When creative mode is on things like flying and infinite resources are allowed.</summary>
		public static bool CreativeMode
		{
			get { return _creativeMode; }
			set
			{
				_creativeMode = value;

				if (IsServer) return;
				if (_creativeMode)
				{
					//turn off options not allowed during creative mode
					InputHost.IsJumping = false;
					BlockCursorHost.MaxDrawDistance = BlockCursorHost.BLOCK_CURSOR_MAX_DRAW_DISTANCE_CREATIVE;
				}
				else
				{
					//turn off options not allowed while in normal mode
					InputHost.IsFloating = false;
					InputHost.IsStandingOnSolidGround = false; //let the input host figure out on the next update event. this prevents a single mid air jump if canceling creative mode while in mid air
					if (Game.UiHost != null) Buttons.SelectTool(ToolType.Default); //reset to default tool while not in creative mode (tools are not available)
					BlockCursorHost.MaxDrawDistance = BlockCursorHost.BLOCK_CURSOR_MAX_DRAW_DISTANCE_NORMAL;
				}
			}
		}

		public static bool Fog { get; set; }

		public static bool InvertMouse { get; set; }

		/// <summary>
		/// Check this property for logic deciding if the current process is running as a standalone server.
		/// True only for standalone servers.
		/// </summary>
		public static bool IsServer { get; private set; }

		/// <summary>
		/// Check this property for logic deciding if actions need to be sent over the network or can just be handled locally.
		/// False for standalone server or client joining a server. True for singleplayer.
		/// </summary>
		public static bool IsSinglePlayer { get; private set; }

		public static string LastWorld { get; set; }

		public static bool LinearMagnificationFilter { get; set; }

		public static bool Maximized { get; set; }

		public static bool Mipmapping { get; set; }

		public static ModeType Mode
		{
			get { return _mode; }
			set
			{
				_mode = value;
				IsSinglePlayer = _mode == ModeType.SinglePlayer;
				IsServer = _mode == ModeType.StandaloneServer;
			}
		}

		public static string MOTD { get; set; }

		public static bool MusicEnabled { get; set; }

		public static ushort Port { get; set; }

		public static string Server { get; set; }

		public static bool SmoothLighting { get; set; }

		public static bool SoundEnabled { get; set; }

		public static string UserName { get; set; }

		/// <summary>View distance in number of chunks.</summary>
		/// <remarks>Minecrafts distances would be: Far=16 (400 or 512 blocks), Normal=8 (256 blocks), Short=4 (128 blocks), Tiny=2 (64 blocks)</remarks>
		public static ViewDistance ViewDistance
		{
			get { return _viewDistance; }
			set
			{
				_viewDistance = value;
				switch (_viewDistance)
				{
					case ViewDistance.Tiny:
						Settings.ZFar = 3 * Chunk.CHUNK_SIZE;
						break;

					case ViewDistance.Low:
						Settings.ZFar = 6 * Chunk.CHUNK_SIZE;
						break;

					case ViewDistance.Standard:
						Settings.ZFar = 10 * Chunk.CHUNK_SIZE;
						break;

					case ViewDistance.High:
						Settings.ZFar = 15 * Chunk.CHUNK_SIZE;
						break;

					case ViewDistance.Extreme:
						Settings.ZFar = 20 * Chunk.CHUNK_SIZE;
						break;

					default: throw new Exception("Unknown View Distance: " + _viewDistance);
				}
			}
		}

		public static bool VSync { get; set; }
		public static bool Windowed { get; set; }
	}
}