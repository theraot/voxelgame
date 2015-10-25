using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Hexpoint.Blox
{
	static class Program
	{
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
		{
			var exception = eventArgs.ExceptionObject as Exception;
			if (exception != null)
			{
				var current = exception;
				do
				{
					Facade.Logbook.Trace
					(
						TraceEventType.Critical,
						" == Unhandled Exception == \n\n{0} ocurred. \n\n == Exception Report == \n\n{1}\n\n == Source == \n\n{2}\n\n == AppDomain == \n\n{3}\n\n == Stacktrace == \n\n{4}\n",
						current.GetType().Name,
						current.Message,
						current.Source,
						AppDomain.CurrentDomain.FriendlyName,
						current.StackTrace
					);
					current = current.InnerException;
				} while (current != null);
				var extendedStackTrace = Environment.StackTrace.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				Facade.Logbook.Trace(TraceEventType.Error, " == Extended StackTrace == \n\n{0}\n\n", string.Join("\r\n", extendedStackTrace, 4, extendedStackTrace.Length - 4));
			}
			else if (eventArgs.ExceptionObject != null)
			{
				Facade.Logbook.Trace
				(
					TraceEventType.Critical,
					" == Unhandled Exception == "
				);
				Facade.Logbook.Trace
				(
					TraceEventType.Critical,
					eventArgs.ExceptionObject.ToString()
				);
			}
			else
			{
				Facade.Logbook.Trace
				(
					TraceEventType.Critical,
					" == Unhandled Exception == "
				);
			}
			Application.Exit();
		}

		[STAThread]
		static void Main(string[] args)
		{
			// Custom exception handling
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); //disables ThreadExceptions so all unhandled exceptions will fire the UnhandledException event (http://stackoverflow.com/questions/2014562/whats-the-difference-between-application-threadexception-and-appdomain-currentd)
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			// TODO: Port GUI to OpenGL
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			System.Threading.Thread.CurrentThread.Name = "Main Thread";

			RemovePatcher();

			Facade.Initialize("Default.log");

			try
			{
				Run(args);
			}
			catch (Exception exception)
			{
				Facade.Logbook.ReportException(exception, true);
			}
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

		private static void Run(string[] args)
		{
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
			// TODO: save Facade.Configuration

			// Exit
			// TODO: Localize text
			Facade.Logbook.Trace(TraceEventType.Information, "Goodbye, see you soon.");
		}
	}
}
