using System;
using System.Diagnostics;
using System.IO;
using System.Security;

namespace Hexpoint.Blox
{
	/// <summary>
	/// The main façade for message logging.
	/// </summary>
	/// <remarks>Logbook is a singleton.
	/// These are the reasons for this:
	/// A) the responsibility of creating Logbook belongs to the caller
	/// B) Logbook should not be responsible of getting the Facade.Configuration to create itself.
	/// C) There will be only one main Logbook per AppDomain. </remarks>
	public class Logbook : MarshalByRefObject
	{
		// NOTE: the listeners are leaked. Listeners are meant to exist for the execution of the process.
		private readonly TraceSource _logSource;

		[SecuritySafeCritical]
		private Logbook(SourceLevels level, bool allowDefaultListener)
		{
			var displayName = Facade.InternalName;
			_logSource = new TraceSource(displayName)
			{
				Switch = new SourceSwitch(displayName)
				{
					Level = level
				}
			};
			if (!allowDefaultListener)
			{
				_logSource.Listeners.Clear();
			}
		}

		/// <summary>
		/// Adds a new listener to the Logbook.
		/// </summary>
		/// <param name="listener">The new listener to be added.</param>
		[SecuritySafeCritical]
		public void AddListener(TraceListener listener)
		{
			_logSource.Listeners.Add(listener);
		}

		/// <summary>
		/// Reports the occurrence of an exception.
		/// </summary>
		/// <param name="exception">The exception to report.</param>
		/// <param name="situation">A description of the situation when the exception happened. (Describe what was being attempted)</param>
		/// <param name="severe">true to indicated this exception will reduce functionality or require extra steps for fixing, false to indicate the program is designed to recover from this.</param>
		/// <remarks>If severe is set to false, the stack trace will not be included.</remarks>
		public void ReportException(Exception exception, string situation, bool severe)
		{
			if (severe)
			{
				var extendedStackTrace = Environment.StackTrace.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				Trace
				(
					TraceEventType.Error,
					"\n\n{0} ocurred while {1}. \n\n == Exception Report == \n\n{2}\n\n == Source == \n\n{3}\n\n == AppDomain == \n\n{4}\n\n == Stacktrace == \n\n{5}\n\n == Extended Stacktrace == \n\n{6}\n",
					exception.GetType().Name,
					situation,
					exception.Message,
					exception.Source,
					AppDomain.CurrentDomain.FriendlyName,
					exception.StackTrace,
					string.Join("\r\n", extendedStackTrace, 4, extendedStackTrace.Length - 4)
				);
			}
			else
			{
				Trace
				(
					TraceEventType.Error,
					"\n\n{0}: {1}\nOcurred while {2}.\n",
					exception.GetType().Name,
					exception.Message,
					situation
				);
			}
		}

		/// <summary>
		/// Reports the occurrence of an exception.
		/// </summary>
		/// <param name="exception">The exception to report.</param>
		/// <param name="severe">true to indicated this exception will reduce functionality or require extra steps for fixing, false to indicate the program is designed to recover from this.</param>
		/// <remarks>If severe is set to false, the stack trace will not be included.</remarks>
		public void ReportException(Exception exception, bool severe)
		{
			if (severe)
			{
				var extendedStackTrace = Environment.StackTrace.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				Trace
				(
					TraceEventType.Error,
					"\n\n{0} ocurred. \n\n == Exception Report == \n\n{1}\n\n == Source == \n\n{2}\n\n == AppDomain == \n\n{3}\n\n == Stacktrace == \n\n{4}\n\n == Extended Stacktrace == \n\n{5}\n",
					exception.GetType().Name,
					exception.Message,
					exception.Source,
					AppDomain.CurrentDomain.FriendlyName,
					exception.StackTrace,
					string.Join("\r\n", extendedStackTrace, 4, extendedStackTrace.Length - 4)
				);
			}
			else
			{
				Trace
					(
					TraceEventType.Error,
					"\n\n{0}: {1}\n",
					exception.GetType().Name,
					exception.Message
				);
			}
		}

		/// <summary>
		/// Writes a message to the logs.
		/// </summary>
		/// <param name="eventType">The relevance of the message.</param>
		/// <param name="format">A composite format string (Composite Formatting) that contains text intermixed with zero or more format items, which correspond to objects in the args array.</param>
		/// <param name="args">An object array containing zero or more objects to format.</param>
		public void Trace(TraceEventType eventType, string format, params object[] args)
		{
			_logSource.TraceEvent(eventType, 0, UtcNowIsoFormat() + " " + format, args);
		}

		/// <summary>
		/// Writes a message to the logs.
		/// </summary>
		/// <param name="eventType">The relevance of the message.</param>
		/// <param name="message">The message to write.</param>
		public void Trace(TraceEventType eventType, string message)
		{
			_logSource.TraceEvent(eventType, 0, UtcNowIsoFormat() + " " + message);
		}


		/// <summary>
		/// Creates a new instance of Logbook if no previous instance is available. Returns the existing (newly created or not) instance.
		/// </summary>
		/// <param name="level">The level for the messages that will be recorded.</param>
		/// <param name="allowDefaultListener">indicated whatever the default listener should be kept or not.</param>
		/// <param name = "logFile">The name of the resource to log to.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CC0022:Should dispose object", Justification = "By Design")]
		internal static Logbook Create(SourceLevels level, bool allowDefaultListener, string logFile)
		{
			// This should be called during initialization.
			// Double initialization is posible if multiple threads attemps to create the logbook...
			// Since that should not happen, let's accept the garbage if somehow that comes to be.
			var result = new Logbook(level, allowDefaultListener);
			try
			{
				// Do not convert to LINQ, foreach is more readable and means less instantiations.
				foreach (char character in Path.GetInvalidFileNameChars())
				{
					logFile = logFile.Replace(character.ToString(), string.Empty);
				}
				var logStreamWriter = new StreamWriter(Facade.Folder + logFile) { AutoFlush = true };
				result.AddListener(new TextWriterTraceListener(logStreamWriter));
			}
			catch (Exception exception)
			{
				// We have failed to create a logbook to which to write logs
#pragma warning disable CC0004 // Catch block cannot be empty
				result.ReportException(exception, "trying to create the log file.", true);
				try
				{
					Console.WriteLine(@"Unable to create log file.");
					Console.WriteLine(@"== Exception Report ==");
					Console.WriteLine(exception.Message);
					Console.WriteLine(@"== Stacktrace ==");
					Console.WriteLine(exception.StackTrace);
				}
				catch (IOException)
				{
					// We have also failed to write to the console, there is no way to report.
					// Ignore.
				}
#pragma warning restore CC0004 // Catch block cannot be empty
			}
			try
			{
				// Test for Console
				GC.KeepAlive(Console.WindowHeight);
				result.AddListener(new ConsoleTraceListener());
			}
			catch (Exception exception)
			{
				result.ReportException(exception, "trying to access the Console", false);
			}
			return result;
		}

		/// <summary>
		/// Changes the level for the messages that will be recorded.
		/// </summary>
		/// <param name="level"></param>
		[SecuritySafeCritical]
		internal void ChangeLevel(SourceLevels level)
		{
			_logSource.Switch.Level = level;
		}

		private static string UtcNowIsoFormat()
		{
			// UtcTime to miliseconds presition.
			// using Z to denote Zero offset.
			// No, that's not the time zone of zulu people.
			return DateTime.UtcNow.ToString("yyyy-MM-dd HH':'mm':'ss'.'fff'Z'");
		}
	}
}