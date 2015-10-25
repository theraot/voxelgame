using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hexpoint.Blox
{
	/// <summary>
	/// Façade to save and load resources.
	/// </summary>
	public class Resources : MarshalByRefObject
	{
		internal Resources()
		{
			// Empty
		}

		public static List<string> GetFolders(string[] prefixes)
		{
			var result = new List<string>();
			foreach (var prefix in prefixes)
			{
				var folder = Facade.Folder + prefix.Replace('.', Path.DirectorySeparatorChar);
				if (Directory.Exists(folder))
				{
					result.Add(folder);
				}
			}
			return result;
		}

		public static List<string> GetFolders(string[] prefixes, bool create)
		{
			var result = new List<string>();
			foreach (var prefix in prefixes)
			{
				var folder = Facade.Folder + prefix.Replace('.', Path.DirectorySeparatorChar);
				if (create && !Directory.Exists(folder))
				{
					try
					{
						Directory.CreateDirectory(folder);
					}
					catch (Exception exception)
					{
						Facade.Logbook.Trace(TraceEventType.Error, "Unable to create folder: {0}", result);
						Facade.Logbook.ReportException(exception, false);
					}
				}
				if (Directory.Exists(folder))
				{
					result.Add(folder);
				}
			}
			return result;
		}

		/// <summary>
		/// Retrieves a bitmap from the resources for the calling assembly.
		/// </summary>
		/// <param name="resourceName"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Stream LoadStream(string resourceName)
		{
			var assembly = Assembly.GetCallingAssembly();
			var stream = Read(assembly, Path.DirectorySeparatorChar + resourceName, new[] { "Images" }, resourceName);
			if (stream == null)
			{
				Facade.Logbook.Trace
				(
					TraceEventType.Error,
					" - Unable to load Bitmap {0}.",
					resourceName
				);
				return null;
			}
			return stream;
		}

		/// <summary>
		/// Reads a resource as an stream.
		/// </summary>
		/// <param name="assembly">The assembly the resource is associated with.</param>
		/// <param name="extension">The extension of the resource to load.</param>
		/// <param name="prefixes">A list of valid prefixes for the resource to load.</param>
		/// <param name="resourceName">The name of the resource to load.</param>
		/// <returns>A readable stream for the resource.</returns>
		internal static Stream Read(Assembly assembly, string extension, string[] prefixes, string resourceName)
		{
			Stream stream;

			foreach (var folder in GetFolders(prefixes))
			{
				if (TryReadStream(folder, extension, assembly, out stream))
				{
					return stream;
				}
			}

			return TryReadDefaultStream(assembly, prefixes, resourceName, out stream) ? stream : null;
		}

		/// <summary>
		/// Writes a resource stream.
		/// </summary>
		/// <param name="assembly">The assembly the resource is associated with.</param>
		/// <param name="prefix">The prefix where to store the resource.</param>
		/// <param name="resourceName">The name of the resource to write.</param>
		/// <param name="stream">A readable stream with the resource to be written.</param>
		/// <returns>true if the resource was written, false otherwise.</returns>
		internal static bool Write(Assembly assembly, string prefix, string resourceName, Stream stream)
		{
			foreach (var create in new[] { false, true })
			{
				foreach (var folder in GetFolders(new[] { prefix }, create))
				{
					if (TryWriteStream(folder, resourceName, assembly, stream))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static void CopyStream(Stream input, Stream output)
		{
			const int INT_Length = 4096;
			var buffer = new byte[4096];
			for (var index = input.Read(buffer, 0, INT_Length); index > 0; index = input.Read(buffer, 0, INT_Length))
			{
				output.Write(buffer, 0, index);
			}
		}

		private static bool TryProcessResource(Assembly assembly, string resource, out Stream stream)
		{
			stream = assembly.GetManifestResourceStream(resource);
			return stream != null;
		}

		private static bool TryReadDefaultStream(Assembly assembly, IEnumerable<string> prefixes, string resourceName, out Stream stream)
		{
			var resources = assembly.GetManifestResourceNames();
			var selectedResources = new List<string>();

			foreach (var prefix in prefixes)
			{
				foreach (var resource in resources)
				{
					if (resource.EndsWith(prefix + "." + resourceName))
					{
						selectedResources.Add(resource);
					}
				}

				selectedResources.Sort((left, right) => left.Length.CompareTo(right.Length));

				foreach (var resource in selectedResources)
				{
					if (TryProcessResource(assembly, resource, out stream))
					{
						Facade.Logbook.Trace
						(
							TraceEventType.Information,
							" - Loaded internal resource {0}",
							resource
						);
						return true;
					}
				}

				selectedResources.Clear();
			}

			stream = null;
			return false;
		}

		private static bool TryReadStream(string basepath, string extension, Assembly assembly, out Stream stream)
		{
			var path = basepath + Path.DirectorySeparatorChar + assembly.GetName().Name + extension;
			try
			{
				Facade.Logbook.Trace
				(
					TraceEventType.Information,
					" - Attempting to read from {0}",
					path
				);
				stream = File.OpenRead(path);
				Facade.Logbook.Trace
				(
					TraceEventType.Information,
					" - Succeed to read from {0}",
					path
				);
				return true;
			}
			catch (IOException exception)
			{
				Facade.Logbook.ReportException(exception, "trying to read resource", false);
				stream = null;
				return false;
			}
		}

		private static bool TryWriteStream(string basepath, string resourceName, Assembly assembly, Stream stream)
		{
			var path = basepath + Path.DirectorySeparatorChar + assembly.GetName().Name + resourceName;
			try
			{
				Facade.Logbook.Trace
				(
					TraceEventType.Information,
					" - Attempting to write to {0}",
					path
				);
				Directory.CreateDirectory(basepath);
				using (var file = File.Open(path, FileMode.Create))
				{
					CopyStream(stream, file);
				}
				Facade.Logbook.Trace
				(
					TraceEventType.Information,
					" - Succeed to write to {0}",
					path
				);
				return true;
			}
			catch (IOException exception)
			{
				Facade.Logbook.ReportException(exception, "trying to write resource", false);
				return false;
			}
		}
	}
}