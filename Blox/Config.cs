using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Hexpoint.Blox
{

	/// <summary>
	/// Use Settings class for values that can be altered but not saved.
	/// Use Config class for values that can be altered and saved.
	/// Use Constants class for values that cannot be altered.
	/// </summary>
	internal static class Config
	{

		internal static DirectoryInfo SaveDirectory;

		internal static void Load()
		{
			var assembly = Assembly.GetCallingAssembly();
			Facade.Logbook.Trace
				(
				TraceEventType.Information,
				"Requested to read configuration for {0}",
				assembly.GetName().Name
			);
			// Will try to read:
			// - ~\Config\AssemblyName.json
			// - %AppData%\InternalName\Config\AssemblyName.json
			// - Assembly!Namespace.Config.default.json

			var stream = Resources.Read(assembly, @".json", new[] { "Config" }, "default.json");
			if (stream != null)
			{
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					var str = reader.ReadToEnd();
					Facade.Configuration = JsonConvert.DeserializeObject<Configuration>(str);
				}
			}

			const string SAVE_FILE_FOLDER_NAME = "SaveFiles";
			SaveDirectory = new DirectoryInfo(Path.Combine(Facade.Folder.FullName, SAVE_FILE_FOLDER_NAME));
			if (!SaveDirectory.Exists) SaveDirectory = Facade.Folder.CreateSubdirectory(SAVE_FILE_FOLDER_NAME);
		}

		internal static void Save()
		{
			try
			{
				var assembly = Assembly.GetCallingAssembly();
				Facade.Logbook.Trace
				(
					TraceEventType.Information,
					"Requested to write configuration for {0}",
					assembly.GetName().Name
				);
				var str = JsonConvert.SerializeObject(Facade.Configuration);
				// Will try to write:
				// - ~\Config\AssemblyName.json
				// - %AppData%\InternalName\Config\AssemblyName.json
				using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
				{
					Resources.Write(assembly, "Config", ".json", stream);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error saving config: " + ex.Message);
			}
		}
	}
}
