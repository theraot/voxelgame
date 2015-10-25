using System;
using System.IO;
using System.Windows.Forms;

namespace Hexpoint.Blox
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			// Custom exception handling
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); //disables ThreadExceptions so all unhandled exceptions will fire the UnhandledException event (http://stackoverflow.com/questions/2014562/whats-the-difference-between-application-threadexception-and-appdomain-currentd)
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			System.Threading.Thread.CurrentThread.Name = "Main Thread";

			RemovePatcher();

			if (args.Length > 0 && args[0] == "server")
			{
				using (var serverConsole = new Server.ServerConsole())
				{
					Application.Run(serverConsole);
				}
			}
			else
			{
				using (var launcher = new Launcher())
				{
					Application.Run(launcher);
				}
			}
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = e.ExceptionObject as Exception;
			if (ex == null)
			{
				return;
			}
			using (var writer = new StreamWriter(Path.Combine(Application.StartupPath, "ErrorLog.txt")))
			{
				writer.WriteLine("Version: {0}", Application.ProductVersion);
				writer.WriteLine("Date: {0:yyyy-MM-dd hh:mm:ss tt}", DateTime.Now);
				writer.WriteLine("Exception: {0}", ex.Message);
				writer.WriteLine(ex.StackTrace); //write stack trace in release mode as a convenience for us, it will be obfuscated anyway in published versions and line numbers arent included without the pdb
				if (ex.InnerException != null)
				{
					writer.WriteLine("Inner Exception: {0}", ex.InnerException.Message);
					writer.WriteLine(ex.InnerException.StackTrace); //write stack trace in release mode as a convenience for us, it will be obfuscated anyway in published versions and line numbers arent included without the pdb					
				}
			}
			Application.Exit();
		}

		private static void RemovePatcher()
		{
			// Remove the patcher here if we find it, this takes care of it whether launching a client or server
#pragma warning disable CC0004 // Catch block cannot be empty
			try
			{
				File.Delete("Patcher.exe");
			}
			catch
			{
				// if this hits an exception let the game run anyway, exception is NOT thrown if the file is not there, so not checking File.Exists avoids one roundtrip to the disk
			}
#pragma warning restore CC0004 // Catch block cannot be empty
		}
	}
}
