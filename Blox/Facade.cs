using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Hexpoint.Blox
{
	/// <summary>
	/// Main façade for the application.
	/// </summary>
	public static class Facade
	{
		internal static DirectoryInfo SaveDirectory;
		private const int INT_Initialized = 2;
		private const int INT_Initializing = 1;
		private const int INT_NotInitialized = 0;
		private static int _status;

		/// <summary>
		/// Gets the current Configuration.
		/// </summary>
		public static Configuration Configuration { get; internal set; }

		/// <summary>
		/// Gets whatever or not the current process ia a Debug build or not.
		/// </summary>
		public static bool DebugMode { get; private set; }

		/// <summary>
		/// Gets the path to the folder from where the application is loaded.
		/// </summary>
		public static DirectoryInfo Folder { get; private set; }

		/// <summary>
		/// Returns the internal name for the application.
		/// </summary>
		/// <remarks>The internal name is the simple name of the assembly.</remarks>
		public static string InternalName { get; private set; }

		/// <summary>
		/// Gets the main Logbook.
		/// </summary>
		public static Logbook Logbook { get; private set; }

		/// <summary>
		/// Gets the language for the current process.
		/// </summary>
		public static string System { get; private set; }

		/// <summary>
		/// Initialize the application.
		/// </summary>
		/// <param name="logFile">The name of the desired log file</param>
		public static void Initialize(string logFile)
		{
			if (Interlocked.CompareExchange(ref _status, INT_Initializing, INT_NotInitialized) == INT_NotInitialized)
			{
				InitializeExtracted(logFile);
				Thread.VolatileWrite(ref _status, INT_Initialized);
			}
		}

		public static void SaveConfiguration()
		{
			try

			{
				var assembly = Assembly.GetCallingAssembly();
				Logbook.Trace
				(
					TraceEventType.Information,
					"Requested to write configuration for {0}",
					assembly.GetName().Name
				);
				var str = JsonConvert.SerializeObject(Configuration);
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

		[SecuritySafeCritical]
		private static void InitializeExtracted(string logFile)
		{
			// Note: this method is not thread safe.

			// *********************************
			// Getting folder and display name
			// *********************************

			var assembly = Assembly.GetExecutingAssembly();

			InternalName = assembly.GetName().Name;

			var location = assembly.Location;

			var folder = Path.GetDirectoryName(location);

			// Let this method throw if Folder is null
			if (folder != null && !folder.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
			{
				// On Windows, if you run from the root directoy it will have a trailing directory separator but will not otherwise... so we addd it
				folder += Path.DirectorySeparatorChar;
			}

			if (folder != null)
			{
				Folder = new DirectoryInfo(folder);
			}

			// *********************************
			// Reading System Language
			// *********************************

			System = CultureInfo.CurrentCulture.TextInfo.CultureName;

			// *********************************
			// Setting debug mode
			// *********************************

			DebugMode = false;
			SetDebugMode();

			// *********************************
			// Creating LogBook
			// *********************************

			Logbook = Logbook.Create(DebugMode ? SourceLevels.All : SourceLevels.Information, true, logFile);

			// *********************************
			// Load Configuration
			// *********************************

			LoadConfiguration();

			// *********************************
			// Reporting
			// *********************************

			if (DebugMode)
			{
				Logbook.Trace(TraceEventType.Information, "[Running debug build]");
			}

			Logbook.Trace(TraceEventType.Information, "Internal name: {0}", assembly.FullName);

			// *********************************
			// Reading main Facade.Configuration
			// *********************************

			// TODO localization
		}

		private static void LoadConfiguration()
		{
			var assembly = Assembly.GetCallingAssembly();
			Logbook.Trace
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
					Configuration = JsonConvert.DeserializeObject<Configuration>(str);
				}
			}

			Settings.Version = new Version(Application.ProductVersion);

			const string SAVE_FILE_FOLDER_NAME = "SaveFiles";
			SaveDirectory = new DirectoryInfo(Path.Combine(Folder.FullName, SAVE_FILE_FOLDER_NAME));
			if (!SaveDirectory.Exists) SaveDirectory = Folder.CreateSubdirectory(SAVE_FILE_FOLDER_NAME);
		}

		[Conditional("DEBUG")]
		private static void SetDebugMode()
		{
			DebugMode = true;
		}
	}
}