using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hexpoint.Blox.Hosts.World;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Utilities
{
	internal static class Misc
	{
		//gm: putting this here for now for lack of a better place, steps cant be done in the ViewDistance property setter because it can get called before we have a GL context.
		//-this is only needed for dynamically changing the view distance after the game is already running
		internal static void ChangeViewDistance(ViewDistance vd)
		{
			Config.ViewDistance = vd;
			Config.Save();
			Settings.Game.CalculateProjectionMatrix();
			if (!Game.Player.EyesUnderWater) SetFogParameters(); //only modify fog distance when players eyes arent under water, otherwise the fog will get updated when they surface
		}

		//gm: putting this here for now for lack of a better place, cant go in the Config.ViewDistance setter because that gets set before we have a GL Context.
		//-this is called when the game loads, and when changing view distance while the game is running
		internal static void SetFogParameters()
		{
			GL.Fog(FogParameter.FogMode, (int)FogMode.Linear); //FogStart and FogEnd params apply to the Linear fog mode
			GL.Fog(FogParameter.FogColor, SkyHost.SkyBottomCurrentColor.ToFloatArray());
			GL.Fog(FogParameter.FogStart, Settings.ZFar * 0.3f); //start fog at 30% of zfar
			GL.Fog(FogParameter.FogEnd, Settings.ZFar - 8); //moving to full fog a quarter chunk before the zfar lets the distance transition out smoother
		}

		public static void FillEnumDropDown(ComboBox ddl, Type t, string defaultItem)
		{
			ddl.DataSource = Enum.GetNames(t);
			if (!string.IsNullOrEmpty(defaultItem)) ddl.SelectedIndex = ddl.FindStringExact(defaultItem);
		}

		public static void FillEnumDropDown(ComboBox ddl, Type t, string nullItemText, string defaultItem)
		{
			var list = new List<string> { nullItemText };
			list.AddRange(Enum.GetNames(t));
			ddl.DataSource = list;
			if (!string.IsNullOrEmpty(defaultItem)) ddl.SelectedIndex = ddl.FindStringExact(defaultItem);
		}

		internal static void MessageError(string message)
		{
			MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		internal static void MessageInfo(string message)
		{
			MessageBox.Show(message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		/// <summary>Call when starting a task using continuation option OnlyOnFaulted. This guarantees there is a non null exception on the task.</summary>
		/// <param name="task">faulted task</param>
		/// <remarks>this didnt work out as well as i hoped, but leaving here for now for reference</remarks>
		internal static void DebugFaultedTask(Task task)
		{
			if (task.Exception == null) return; //check anyway to avoid warning
			//display the most specific exceptions messages, could also log these if need be
			var message = new StringBuilder("Task error: ");
			foreach (Exception ex in task.Exception.InnerExceptions)
			{
				message.AppendLine(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
				Debug.WriteLine(message);
			}
		}

		#region Screenshot
		/// <summary>Captures a bitmap from the contents of the current frame buffer. Saves the capture to disk.</summary>
		public static void CaptureScreenshot(GameWindow gameWindow)
		{
			if (OpenTK.Graphics.GraphicsContext.CurrentContext == null) throw new OpenTK.Graphics.GraphicsContextMissingException();

			var bmp = new Bitmap(gameWindow.Width, gameWindow.Height);
			BitmapData data = bmp.LockBits(gameWindow.ClientRectangle, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			GL.ReadPixels(0, 0, gameWindow.Width, gameWindow.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
			bmp.UnlockBits(data);
			bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

			string screenshotPath = string.Format("{0}{1}{2:yyyy-MM-dd} Screenshot.png", Config.AppDirectory.FullName, System.IO.Path.DirectorySeparatorChar, DateTime.Today); //use System.IO.Path.DirectorySeparatorChar to play nice with linux
			try
			{
				bmp.Save(screenshotPath, ImageFormat.Png);
				Debug.WriteLine("Screenshot saved to " + screenshotPath);
			}
			catch (Exception ex)
			{
				throw new Exception("Error while saving screenshot: " + ex.Message);
			}
			finally { bmp.Dispose(); }
		}
		#endregion
	}
}