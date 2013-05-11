using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Utilities
{
	/// <summary>OpenGL Diagnostics.</summary>
	/// <remarks>Some properties in this class will have no usages because they are only used in release mode.</remarks>
	internal static class Diagnostics
	{
		private const int REQUIRED_GL_VERSION_MAJOR = 2;
		private const int REQUIRED_GL_VERSION_MINOR = 0;

		/// <summary>Load some diagnostic properties in advance, such as OpenGL info, because they cant be queried if the game window crashes.</summary>
		public static void LoadDiagnosticProperties()
		{
			OpenGlVersion = GL.GetString(StringName.Version);
			OpenGlVendor = GL.GetString(StringName.Vendor);
			OpenGlGlsl = GL.GetString(StringName.ShadingLanguageVersion);
			OpenGlRenderer = GL.GetString(StringName.Renderer);
			OpenGlExtensions = new List<string>(GL.GetString(StringName.Extensions).Split(' '));

			int major, minor;
			GL.GetInteger(GetPName.MajorVersion, out major);
			GL.GetInteger(GetPName.MinorVersion, out minor);
			if (major == 0) //this can happen on older comps / OpenGL versions (KNIGHT for example running OpenGL 2.1), so we need to parse the major/minor from the version string
			{
				var args = OpenGlVersion.Split('.');
				if (args.Length < 2) throw new Exception("Unable to determine OpenGL from version string: " + OpenGlVersion);
				major = int.Parse(args[0]);
				minor = int.Parse(args[1]);
			}
			if (major <= REQUIRED_GL_VERSION_MAJOR && minor < REQUIRED_GL_VERSION_MINOR) throw new Exception(string.Format("OpenGL version {0}.{1} is required. You are running OpenGL version {2}.{3} This probably means you are using an outdated video card driver.", REQUIRED_GL_VERSION_MAJOR, REQUIRED_GL_VERSION_MINOR, major, minor));
			if (!SupportsVertexBufferObjectExtension) throw new Exception("Vertex Buffer Object (VBO) extension is not supported. This probably means you are using an outdated video card driver."); //its redundant to check for this if we require OpenGL 1.5, however leaving here in case we lower our required version
			if (!SupportsTextureNonPowerOfTwoExtension) throw new Exception("Texture Non Power Of Two (NPOT) extension is not supported. This probably means you are using an outdated video card driver."); //its redundant to check for this if we require OpenGL 2.0, however leaving here in case we lower our required version
			Debug.WriteLineIf(Config.ViewDistance == ViewDistance.Extreme, "Warning: Using Extreme view distance"); //reminder for testing, performance can be greatly impacted
		}

		public static void OutputDebugInfo()
		{
			Debug.WriteLine(OpenGlInfo());
			Debug.WriteLine("Supports GL_ARB_vertex_buffer_object (VBO): {0}", SupportsVertexBufferObjectExtension);
			Debug.WriteLine("Supports GL_ARB_texture_non_power_of_two (NPOT): {0}", SupportsTextureNonPowerOfTwoExtension);

			//use this to display all available monitors and resolutions etc.
			//byte deviceCounter = 0;
			//foreach (DisplayDevice device in DisplayDevice.AvailableDisplays)
			//{
			//    deviceCounter++;
			//    Debug.WriteLine("device #{0}: primary {1}, bounds {2}, refresh {3}, bpp {4}", deviceCounter, device.IsPrimary, device.Bounds, device.RefreshRate, device.BitsPerPixel);
			//    //foreach (OpenTK.DisplayResolution res in device.AvailableResolutions) Debug.WriteLine(res); //displays all available resolutions for the device
			//}
		}

		public static string OpenGlInfo()
		{
			//this is a single line string so it can display nicely when using the /opengl slash command
			return string.Format("OpenGL {0}, Vendor {1}, GLSL {2}, Renderer {3}", OpenGlVersion, OpenGlVendor, OpenGlGlsl, OpenGlRenderer);
		}

		public static string OpenGlVersion { get; private set; }
		public static string OpenGlVendor { get; private set; }
		public static string OpenGlGlsl { get; private set; }
		public static string OpenGlRenderer { get; private set; }
		/// <summary>Space separated list of supported OpenGL extensions.</summary>
		public static List<string> OpenGlExtensions { get; private set; }

		public static bool SupportsExtension(string extension)
		{
			return OpenGlExtensions.Contains(extension);
		}

		/// <summary>Introduced in OpenGL version 1.5</summary>
		public static bool SupportsVertexBufferObjectExtension { get { return SupportsExtension("GL_ARB_vertex_buffer_object"); } }
		/// <summary>Introduced in OpenGL version 2.0</summary>
		public static bool SupportsTextureNonPowerOfTwoExtension { get { return SupportsExtension("GL_ARB_texture_non_power_of_two"); } }

		/// <summary>
		/// Table in the remarks can be used to determine the Windows version using the reported version string.
		/// Mono on Unix/Linux/Apple will report Unix.
		/// </summary>
		/// <remarks>
		///+-----------------------------------------------------------------------------------------------------------------------------------------+
		///|           |   Windows    |   Windows    |   Windows    |Windows NT| Windows | Windows | Windows | Windows | Windows | Windows | Windows |
		///|           |     95       |      98      |     Me       |    4.0   |  2000   |   XP    |  2003   |  Vista  |  2008   |    7    | 2008 R2 |
		///+-----------------------------------------------------------------------------------------------------------------------------------------+
		///|PlatformID | Win32Windows | Win32Windows | Win32Windows | Win32NT  | Win32NT | Win32NT | Win32NT | Win32NT | Win32NT | Win32NT | Win32NT |
		///+-----------------------------------------------------------------------------------------------------------------------------------------+
		///|Major      |              |              |              |          |         |         |         |         |         |         |         |
		///| version   |      4       |      4       |      4       |    4     |    5    |    5    |    5    |    6    |    6    |    6    |    6    |
		///+-----------------------------------------------------------------------------------------------------------------------------------------+
		///|Minor      |              |              |              |          |         |         |         |         |         |         |         |
		///| version   |      0       |     10       |     90       |    0     |    0    |    1    |    2    |    0    |    0    |    1    |    1    |
		///+-----------------------------------------------------------------------------------------------------------------------------------------+
		/// </remarks>
		public static string OperatingSystem { get { return Environment.OSVersion.VersionString; } }
	}
}