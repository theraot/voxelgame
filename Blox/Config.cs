using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Hexpoint.Blox.Hosts;
using Hexpoint.Blox.Hosts.Input;
using Hexpoint.Blox.Hosts.Ui;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox
{
	internal enum ModeType : byte { SinglePlayer, StandaloneServer, JoinServer }
	internal enum ViewDistance : byte { Tiny, Low, Standard, High, Extreme }

	/// <summary>
	/// Use Settings class for values that can be altered but not saved.
	/// Use Config class for values that can be altered and saved.
	/// Use Constants class for values that cannot be altered.
	/// </summary>
	/// <remarks>
	/// For adding new types in the xsd, use this table to match the .net type to the type in the xsd file: http://msdn.microsoft.com/en-us/library/aa719879%28v=vs.71%29.aspx
	/// </remarks>
	internal static class Config
	{
		#region Properties (Saved)
		private static ModeType _mode;
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
		// ReSharper disable InconsistentNaming
		private static string _MOTD;
		// ReSharper restore InconsistentNaming

		private static ViewDistance _viewDistance;
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

		private static bool _creativeMode;
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

		#endregion

		#region Properties (Static)
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

		private static string _configFilePath;
		private static XmlDocument _configXml;
		internal static DirectoryInfo AppDirectory;
		internal static DirectoryInfo SaveDirectory;
		#endregion

		#region Operations
		internal static void Load()
		{
			try
			{
				AppDirectory = new DirectoryInfo(Application.StartupPath);
				if (AppDirectory == null) throw new Exception(string.Format("Failed to retrieve app directory info for: {0}", Application.StartupPath));
				_configFilePath = Path.Combine(AppDirectory.FullName, "Config.xml"); //use Path.Combine to play nice with linux
				_configXml = new XmlDocument();

				if (File.Exists(_configFilePath)) //config file exists, load it
				{
					_configXml.Load(_configFilePath);
				}
				else //no config file, use defaults
				{
					_configXml.LoadXml("<?xml version=\"1.0\" ?>\n<Config />");
				}
				_configXml.Schemas.Add("", XmlReader.Create(new StringReader(Properties.Resources.Config)));
				_configXml.Validate(null);

				_userName = LoadSetting("UserName");
				_server = LoadSetting("Server");
				_port = LoadSetting("Port", Server.Controller.TCP_LISTENER_PORT);
				_lastWorld = LoadSetting("LastWorld");

				ModeType modeType;
				Mode = Enum.TryParse(LoadSetting("Mode"), out modeType) ? modeType : ModeType.SinglePlayer; //if the enum value in the config file is invalid then default it without failing

				_windowed = LoadSetting("Windowed", true);
				_maximized = LoadSetting("Maximized", true);
				_invertMouse = LoadSetting("InvertMouse", false);
				_vSync = LoadSetting("VSync", true);
				_mipmapping = LoadSetting("Mipmapping", true);
				_fog = LoadSetting("Fog", true);
				_linearMagnificationFilter = LoadSetting("LinearMagnificationFilter", false);
				_smoothLighting = LoadSetting("SmoothLighting", true);
				_MOTD = LoadSetting("MOTD");

				ViewDistance vd;
				ViewDistance = Enum.TryParse(LoadSetting("ViewDistance"), out vd) ? vd : ViewDistance.Standard; //if the enum value in the config file is invalid then default it without failing

				_soundEnabled = LoadSetting("SoundEnabled", true);
				_musicEnabled = LoadSetting("MusicEnabled", true);
				CreativeMode = LoadSetting("CreativeMode", false);

				const string SAVE_FILE_FOLDER_NAME = "SaveFiles";
				SaveDirectory = new DirectoryInfo(Path.Combine(AppDirectory.FullName, SAVE_FILE_FOLDER_NAME));
				if (!SaveDirectory.Exists) SaveDirectory = AppDirectory.CreateSubdirectory(SAVE_FILE_FOLDER_NAME);

				//set version here so the game window has access to it and so the server also loads it when starting in a new process
				Settings.Version = new Version(Application.ProductVersion);
			}
			catch (Exception ex)
			{
				Utilities.Misc.MessageError(string.Format("Error loading config, if the problem persists, try removing your Config.xml file.\n\n{0}", ex.GetBaseException().Message));
				Application.Exit(); //weird things can happen if config doesnt load properly so just exit, the client gets a nice enough message that they should be able to figure out the problem
			}
		}

		private static string LoadSetting(string name, string defaultValue = "")
		{
			var node = _configXml.SelectSingleNode("//" + name);
			return node != null ? node.InnerText : defaultValue;
		}

		private static bool LoadSetting(string name, bool defaultValue)
		{
			var node = _configXml.SelectSingleNode("//" + name);
			return node != null ? XmlConvert.ToBoolean(node.InnerText) : defaultValue;
		}

		private static ushort LoadSetting(string name, ushort defaultValue)
		{
			var node = _configXml.SelectSingleNode("//" + name);
			return node != null ? XmlConvert.ToUInt16(node.InnerText) : defaultValue;
		}

		internal static void Save()
		{
			try
			{
				SaveSetting("UserName", _userName);
				SaveSetting("Server", _server);
				SaveSetting("Port", _port.ToString());
				SaveSetting("LastWorld", _lastWorld);
				SaveSetting("Mode", Mode.ToString());
				SaveSetting("Windowed", _windowed);
				SaveSetting("Maximized", _maximized);
				SaveSetting("InvertMouse", _invertMouse);
				SaveSetting("VSync", _vSync);
				SaveSetting("Mipmapping", _mipmapping);
				SaveSetting("Fog", _fog);
				SaveSetting("LinearMagnificationFilter", _linearMagnificationFilter);
				SaveSetting("SmoothLighting", _smoothLighting);
				SaveSetting("MOTD", _MOTD);
				SaveSetting("ViewDistance", ViewDistance.ToString());
				SaveSetting("SoundEnabled", _soundEnabled);
				SaveSetting("MusicEnabled", _musicEnabled);
				SaveSetting("CreativeMode", CreativeMode);

				_configXml.Save(_configFilePath);
			}
			catch (Exception ex)
			{
				throw new Exception("Error saving config: " + ex.Message);
			}
		}

		private static void SaveSetting(string name, string value)
		{
			// ReSharper disable PossibleNullReferenceException
			var node = _configXml.SelectSingleNode("//" + name) ?? _configXml.DocumentElement.AppendChild(_configXml.CreateNode(XmlNodeType.Element, name, ""));
			// ReSharper restore PossibleNullReferenceException
			node.InnerText = value;
		}

		private static void SaveSetting(string name, bool value)
		{
			// ReSharper disable PossibleNullReferenceException
			var node = _configXml.SelectSingleNode("//" + name) ?? _configXml.DocumentElement.AppendChild(_configXml.CreateNode(XmlNodeType.Element, name, ""));
			// ReSharper restore PossibleNullReferenceException
			node.InnerText = XmlConvert.ToString(value);
		}
		#endregion
	}
}
