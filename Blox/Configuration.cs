using System;
using Hexpoint.Blox.Hosts;
using Hexpoint.Blox.Hosts.Input;
using Hexpoint.Blox.Hosts.Ui;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox
{
	internal static class Configuration
	{
		private static ModeType _mode;
		private static ViewDistance _viewDistance;
		private static bool _creativeMode;
		private static string _userName;
		private static string _server;
		private static ushort _port;
		private static string _lastWorld;
		private static bool _soundEnabled;
		private static bool _musicEnabled;
		private static bool _windowed;
		private static bool _maximized;
		private static bool _invertMouse;
		private static bool _vSync;
		private static bool _mipmapping;
		private static bool _fog;
		private static bool _linearMagnificationFilter;
		private static bool _smoothLighting;
		private static string _MOTD;

		/// <summary>
		/// Check this property for logic deciding if actions need to be sent over the network or can just be handled locally.
		/// False for standalone server or client joining a server. True for singleplayer.
		/// </summary>
		internal static bool IsSinglePlayer;

		/// <summary>
		/// Check this property for logic deciding if the current process is running as a standalone server.
		/// True only for standalone servers.
		/// </summary>
		internal static bool IsServer;

		internal static ModeType Mode
		{
			get { return _mode; }
			set
			{
				_mode = value;
				IsSinglePlayer = _mode == ModeType.SinglePlayer;
				IsServer = _mode == ModeType.StandaloneServer;
			}
		}

		/// <summary>View distance in number of chunks.</summary>
		/// <remarks>Minecrafts distances would be: Far=16 (400 or 512 blocks), Normal=8 (256 blocks), Short=4 (128 blocks), Tiny=2 (64 blocks)</remarks>
		internal static ViewDistance ViewDistance
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

		/// <summary>When creative mode is on things like flying and infinite resources are allowed.</summary>
		internal static bool CreativeMode
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

		internal static string UserName
		{
			get { return _userName; }
			set { _userName = value; }
		}

		internal static string Server1
		{
			get { return _server; }
			set { _server = value; }
		}

		internal static ushort Port
		{
			get { return _port; }
			set { _port = value; }
		}

		internal static string LastWorld
		{
			get { return _lastWorld; }
			set { _lastWorld = value; }
		}

		internal static bool SoundEnabled
		{
			get { return _soundEnabled; }
			set { _soundEnabled = value; }
		}

		internal static bool MusicEnabled
		{
			get { return _musicEnabled; }
			set { _musicEnabled = value; }
		}

		internal static bool Windowed
		{
			get { return _windowed; }
			set { _windowed = value; }
		}

		internal static bool Maximized
		{
			get { return _maximized; }
			set { _maximized = value; }
		}

		internal static bool InvertMouse
		{
			get { return _invertMouse; }
			set { _invertMouse = value; }
		}

		internal static bool VSync
		{
			get { return _vSync; }
			set { _vSync = value; }
		}

		internal static bool Mipmapping
		{
			get { return _mipmapping; }
			set { _mipmapping = value; }
		}

		internal static bool Fog
		{
			get { return _fog; }
			set { _fog = value; }
		}

		internal static bool LinearMagnificationFilter
		{
			get { return _linearMagnificationFilter; }
			set { _linearMagnificationFilter = value; }
		}

		internal static bool SmoothLighting
		{
			get { return _smoothLighting; }
			set { _smoothLighting = value; }
		}

		internal static string MOTD
		{
			get { return _MOTD; }
			set { _MOTD = value; }
		}
	}
}