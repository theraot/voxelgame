using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace Hexpoint.Blox
{

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
		internal static DirectoryInfo SaveDirectory;
		#endregion

		#region Operations
		internal static void Load()
		{
			try
			{
				// TODO: null Facade.Folder
				_configFilePath = Path.Combine(Facade.Folder.FullName, "Config.xml"); //use Path.Combine to play nice with linux
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

				Facade.Configuration.UserName = LoadSetting("UserName");
				Facade.Configuration.Server = LoadSetting("Server");
				Facade.Configuration.Port = LoadSetting("Port", Server.Controller.TCP_LISTENER_PORT);
				Facade.Configuration.LastWorld = LoadSetting("LastWorld");

				ModeType modeType;
				Facade.Configuration.Mode = Enum.TryParse(LoadSetting("Mode"), out modeType) ? modeType : ModeType.SinglePlayer; //if the enum value in the config file is invalid then default it without failing

				Facade.Configuration.Windowed = LoadSetting("Windowed", true);
				Facade.Configuration.Maximized = LoadSetting("Maximized", true);
				Facade.Configuration.InvertMouse = LoadSetting("InvertMouse", false);
				Facade.Configuration.VSync = LoadSetting("VSync", true);
				Facade.Configuration.Mipmapping = LoadSetting("Mipmapping", true);
				Facade.Configuration.Fog = LoadSetting("Fog", true);
				Facade.Configuration.LinearMagnificationFilter = LoadSetting("LinearMagnificationFilter", false);
				Facade.Configuration.SmoothLighting = LoadSetting("SmoothLighting", true);
				Facade.Configuration.MOTD = LoadSetting("MOTD");

				ViewDistance vd;
				Facade.Configuration.ViewDistance = Enum.TryParse(LoadSetting("ViewDistance"), out vd) ? vd : ViewDistance.Standard; //if the enum value in the config file is invalid then default it without failing

				Facade.Configuration.SoundEnabled = LoadSetting("SoundEnabled", true);
				Facade.Configuration.MusicEnabled = LoadSetting("MusicEnabled", true);
				Facade.Configuration.CreativeMode = LoadSetting("CreativeMode", false);

				const string SAVE_FILE_FOLDER_NAME = "SaveFiles";
				// TODO: null Facade.Folder
				SaveDirectory = new DirectoryInfo(Path.Combine(Facade.Folder.FullName, SAVE_FILE_FOLDER_NAME));
				if (!SaveDirectory.Exists) SaveDirectory = Facade.Folder.CreateSubdirectory(SAVE_FILE_FOLDER_NAME);

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
				SaveSetting("UserName", Facade.Configuration.UserName);
				SaveSetting("Server", Facade.Configuration.Server);
				SaveSetting("Port", Facade.Configuration.Port.ToString());
				SaveSetting("LastWorld", Facade.Configuration.LastWorld);
				SaveSetting("Mode", Facade.Configuration.Mode.ToString());
				SaveSetting("Windowed", Facade.Configuration.Windowed);
				SaveSetting("Maximized", Facade.Configuration.Maximized);
				SaveSetting("InvertMouse", Facade.Configuration.InvertMouse);
				SaveSetting("VSync", Facade.Configuration.VSync);
				SaveSetting("Mipmapping", Facade.Configuration.Mipmapping);
				SaveSetting("Fog", Facade.Configuration.Fog);
				SaveSetting("LinearMagnificationFilter", Facade.Configuration.LinearMagnificationFilter);
				SaveSetting("SmoothLighting", Facade.Configuration.SmoothLighting);
				SaveSetting("MOTD", Facade.Configuration.MOTD);
				SaveSetting("ViewDistance", Facade.Configuration.ViewDistance.ToString());
				SaveSetting("SoundEnabled", Facade.Configuration.SoundEnabled);
				SaveSetting("MusicEnabled", Facade.Configuration.MusicEnabled);
				SaveSetting("CreativeMode", Facade.Configuration.CreativeMode);

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
