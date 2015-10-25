using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;

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

		#region Properties (Static)

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

				Configuration.UserName = LoadSetting("UserName");
				Configuration.Server1 = LoadSetting("Server");
				Configuration.Port = LoadSetting("Port", Server.Controller.TCP_LISTENER_PORT);
				Configuration.LastWorld = LoadSetting("LastWorld");

				ModeType modeType;
				Configuration.Mode = Enum.TryParse(LoadSetting("Mode"), out modeType) ? modeType : ModeType.SinglePlayer; //if the enum value in the config file is invalid then default it without failing

				Configuration.Windowed = LoadSetting("Windowed", true);
				Configuration.Maximized = LoadSetting("Maximized", true);
				Configuration.InvertMouse = LoadSetting("InvertMouse", false);
				Configuration.VSync = LoadSetting("VSync", true);
				Configuration.Mipmapping = LoadSetting("Mipmapping", true);
				Configuration.Fog = LoadSetting("Fog", true);
				Configuration.LinearMagnificationFilter = LoadSetting("LinearMagnificationFilter", false);
				Configuration.SmoothLighting = LoadSetting("SmoothLighting", true);
				Configuration.MOTD = LoadSetting("MOTD");

				ViewDistance vd;
				Configuration.ViewDistance = Enum.TryParse(LoadSetting("ViewDistance"), out vd) ? vd : ViewDistance.Standard; //if the enum value in the config file is invalid then default it without failing

				Configuration.SoundEnabled = LoadSetting("SoundEnabled", true);
				Configuration.MusicEnabled = LoadSetting("MusicEnabled", true);
				Configuration.CreativeMode = LoadSetting("CreativeMode", false);

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
				SaveSetting("UserName", Configuration.UserName);
				SaveSetting("Server", Configuration.Server1);
				SaveSetting("Port", Configuration.Port.ToString());
				SaveSetting("LastWorld", Configuration.LastWorld);
				SaveSetting("Mode", Configuration.Mode.ToString());
				SaveSetting("Windowed", Configuration.Windowed);
				SaveSetting("Maximized", Configuration.Maximized);
				SaveSetting("InvertMouse", Configuration.InvertMouse);
				SaveSetting("VSync", Configuration.VSync);
				SaveSetting("Mipmapping", Configuration.Mipmapping);
				SaveSetting("Fog", Configuration.Fog);
				SaveSetting("LinearMagnificationFilter", Configuration.LinearMagnificationFilter);
				SaveSetting("SmoothLighting", Configuration.SmoothLighting);
				SaveSetting("MOTD", Configuration.MOTD);
				SaveSetting("ViewDistance", Configuration.ViewDistance.ToString());
				SaveSetting("SoundEnabled", Configuration.SoundEnabled);
				SaveSetting("MusicEnabled", Configuration.MusicEnabled);
				SaveSetting("CreativeMode", Configuration.CreativeMode);

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
