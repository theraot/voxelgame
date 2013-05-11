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
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			//custom exception handling
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); //disables ThreadExceptions so all unhandled exceptions will fire the UnhandledException event (http://stackoverflow.com/questions/2014562/whats-the-difference-between-application-threadexception-and-appdomain-currentd)
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			System.Threading.Thread.CurrentThread.Name = "Main Thread";

			//remove the patcher here if we find it, this takes care of it whether launching a client or server
			// ReSharper disable EmptyGeneralCatchClause
			try { File.Delete("Patcher.exe"); } catch { } //if this hits an exception let the game run anyway, exception is NOT thrown if the file is not there, so not checking File.Exists avoids one roundtrip to the disk
			// ReSharper restore EmptyGeneralCatchClause

			if (args.Length > 0 && args[0] == "server")
			{
				Application.Run(new Server.ServerConsole());
			}
			else
			{
				Application.Run(new Launcher());
			}
		}

		/// <summary>Log all unhandled exceptions from the client or server. Write them to ErrorLog.txt file. Exceptions handled in any way, ie: in the WinForms msgbox will not end up here, only exceptions that cause a hard crash.</summary>
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = e.ExceptionObject as Exception;
			if (ex == null) return;
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
	}
}
