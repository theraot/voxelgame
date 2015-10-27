using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
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

		private static readonly object _synclock = new object();
		private static volatile bool _done;
		private static bool _lockTaken;

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
		public static string SystemLanguage { get; private set; }

		/// <summary>
		/// Gets the localized texts for the current language
		/// </summary>
		public static Func<string, object, string> Texts { get; internal set; }

		/// <summary>
		/// Initialize the application.
		/// </summary>
		/// <param name="logFile">The name of the desired log file</param>
		public static void Initialize(string logFile)
		{
			// Will be in this method until the job is done (so after the method returns* we know it is done).
			// *: When the method returns it is done, but not when it throws.
			while (!_done)
			{
				bool lockOwned;
				try
				{
					// Only one thread will get the lock.
					// After the lock is taken, any subsequent thread will get an exception.
					// But there is a chance that many threads will pass without exception.
					Monitor.TryEnter(_synclock, ref _lockTaken);
					// If multiple threads passed without exception, only one of then has the lock.
					// The thread that has the lock can take it again.
					lockOwned = Monitor.TryEnter(_synclock);
					if (lockOwned)
					{
						// The current thread took the lock again.
						// Release the lock once.
						Monitor.Exit(_synclock);
					}
				}
				catch (ArgumentException)
				{
					// Since the current thread doesn't have the lock, it must wait until the job is done.
					lockOwned = false;
				}
				// If the current thread has the lock...
				if (lockOwned)
				{
					try
					{
						// Do the job.
						InitializeExtracted(logFile);
						// Report that the job is done.
						_done = true;
						// return.
						return;
					}
					catch (Exception)
					{
						// Failed to do the job, we need another thread to get the lock.
						_lockTaken = false;
						// Let it throw for logs.
						throw;
					}
					finally
					{
						// Regardless of success of failure...
						// Release the lock.
						Monitor.Exit(_synclock);
					}
				}
				// The block above did return or throw, so here we know that the current thread doesn't have the lock.
				while (_lockTaken)
				{
					if (_done)
					{
						// The job is already done, return.
						return;
					}
					// The job is yet to be done, yield the time slice for another thread to proceed.
					Thread.Yield();
				}
				// The lock has been released, this means that the thread that had the lock has failed.
				// Loop to try again to get the lock.
			}
		}

		public static void LocalizeControl(Control control)
		{
			var _ = Texts;
			control.Text = _(control.Text, null);
			var property = TypeDescriptor.GetProperties(control).Find("Controls", false);
			if (property != null)
			{
				var controls = property.GetValue(control);
				var controlCollection = controls as Control.ControlCollection;
				if (controlCollection != null)
				{
					foreach (Control subControl in controlCollection)
					{
						LocalizeControl(subControl);
					}
				}
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
				// - ~\AssemblyName\Lang\config.json
				using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
				{
					Resources.Write(assembly, ".json", null, "config", stream);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error saving config: " + ex.Message);
			}
		}

		private static Dictionary<string, string> GetLocalizedTexts(string language)
		{
			var assembly = Assembly.GetCallingAssembly();
			Logbook.Trace
				(
				TraceEventType.Information,
				"Requested to read localized texts for {0}",
				assembly.GetName().Name
			);
			// Will try to read:
			// - ~\AssemblyName\Lang\langcode.json
			// - Assembly!Namespace.Lang.langcode.json
			var languageArray = language.Split('-');
			var resourceNames = new List<string>();
			var composite = new StringBuilder();
			foreach (var sublanguage in languageArray)
			{
				if (composite.Length > 0)
				{
					composite.Append("-");
				}
				composite.Append(sublanguage.Trim());
				resourceNames.Add(composite.ToString());
			}
			resourceNames.Reverse();
			var stream = Resources.Read(assembly, ".json", new[] { "Lang" }, resourceNames.ToArray());
			if (stream == null)
			{
				Logbook.Trace
					(
					TraceEventType.Information,
					"No localized texts for {0}",
					assembly.GetName().Name
				);
				return null;
			}
			using (var reader = new StreamReader(stream, Encoding.UTF8))
			{
				var str = reader.ReadToEnd();
				return JsonConvert.DeserializeObject<Dictionary<string, string>>(str);
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

			SystemLanguage = CultureInfo.CurrentCulture.TextInfo.CultureName;

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

			Texts = LoadTexts(SystemLanguage);
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
			// - ~\AssemblyName\config.json
			// - Assembly!Namespace.config.json

			var stream = Resources.Read(assembly, ".json", null, "config");
			if (stream != null)
			{
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					var str = reader.ReadToEnd();
					Configuration = JsonConvert.DeserializeObject<Configuration>(str);
				}
			}
			if (Configuration == null)
			{
				// On failure load default
				Configuration = new Configuration();
			}

			Settings.Version = new Version(Application.ProductVersion);

			const string SAVE_FILE_FOLDER_NAME = "SaveFiles";
			SaveDirectory = new DirectoryInfo(Path.Combine(Folder.FullName, SAVE_FILE_FOLDER_NAME));
			if (!SaveDirectory.Exists) SaveDirectory = Folder.CreateSubdirectory(SAVE_FILE_FOLDER_NAME);
		}

		/// <summary>
		/// Retrieve a LocalizedTexts with the localized texts for the calling assembly.
		/// </summary>
		/// <param name="language">The language for which to load the texts.</param>
		/// <returns>a new LocalizedTexts object for the calling assembly</returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Func<string, object, string> LoadTexts(string language)
		{
			var dictionary = GetLocalizedTexts(language);
			if (dictionary == null)
			{
				// Keep the lambda notation - it is tempting to try to simplify this line... don't.
				return (format, source) => format.FormatWith(source);
			}
			return (format, source) =>
			{
				string result;
				if (dictionary.TryGetValue(format, out result))
				{
					if (source != null)
					{
						return result.FormatWith(source);
					}
					return result;
				}
				return format;
			};
		}

		[Conditional("DEBUG")]
		private static void SetDebugMode()
		{
			DebugMode = true;
		}
	}
}