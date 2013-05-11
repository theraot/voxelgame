using System;
using System.Diagnostics;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Hosts
{
	internal class PerformanceHost : IHost
	{
		#region Events
		public delegate void UpdateCyclingHandler();
		/// <summary>Event fired at the end of every update loop cycle and half way through. Fires about every half second.</summary>
		public static event UpdateCyclingHandler OnHalfSecondElapsed;
		/// <summary>Event fired at the end of every update loop cycle about once per second.</summary>
		public static event UpdateCyclingHandler OnSecondElapsed;
		/// <summary>Event fired at the end of every 5 update loop cycles. About once per 5 seconds.</summary>
		public static event UpdateCyclingHandler OnFiveSecondsElapsed;
		/// <summary>Event fired at the end of every 10 update loop cycles. About once per 10 seconds.</summary>
		public static event UpdateCyclingHandler OnTenSecondsElapsed;

		private int _secondsElapsed;
		#endregion

		/// <summary>
		/// Current FPS for the most recent second. Divides frames rendered by the total update time taken
		/// during the previous second in case the game is running slower then the update rate is set at.
		/// </summary>
		public short Fps;

		/// <summary>Current total memory usage from the most recent second.</summary>
		public short Memory;

		/// <summary>Total number of chunks rendered in the most recent frame.</summary>
		public int ChunksRendered;

		/// <summary>Total number of chunks not in the "NotLoaded" state.</summary>
		public int ChunksInMemory;

		/// <summary>Total number of active threads.</summary>
		public int ActiveThreads;

		/// <summary>Total number of threads.</summary>
		public int TotalThreads;

		/// <summary>Use this for anything that should be altered back and forth each second.</summary>
		/// <remarks>example usage is for blinking text input cursor</remarks>
		public bool IsAlternateSecond;

		private int _updateCount;
		private double _updateTime;
		private int _framesRendered;
		private const int UPDATE_HALFWAY_POINT = Constants.UPDATES_PER_SECOND / 2;

		public void Update(FrameEventArgs e)
		{
			_updateCount++;
			_updateTime += e.Time;
			if (_updateCount == UPDATE_HALFWAY_POINT) OnHalfSecondElapsed(); //raise the half second event
			if (_updateCount < Constants.UPDATES_PER_SECOND) return;

			//perform all tasks that happen only once per second
			IsAlternateSecond = !IsAlternateSecond;
			Fps = (short)(_framesRendered / _updateTime - 1); //assign the actual fps recorded during the previous second, subtracting one to be more accurate (would always say 61 with vsync on otherwise)
			_updateCount = 0;
			_updateTime = 0;
			_framesRendered = 0;

			if (OnHalfSecondElapsed != null) OnHalfSecondElapsed(); //raise the half second event
			if (OnSecondElapsed != null) OnSecondElapsed(); //raise the cycle event
			unchecked { _secondsElapsed++; }
			if (OnFiveSecondsElapsed != null && _secondsElapsed % 5 == 0) OnFiveSecondsElapsed(); //raise the 5 seconds elapsed event
			if (OnTenSecondsElapsed != null && _secondsElapsed % 10 == 0) OnTenSecondsElapsed(); //raise the 10 seconds elapsed event

			if (IsAlternateSecond) //check some things every 2 seconds
			{
				Memory = (short)(Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024);
				if (Config.IsSinglePlayer && !Settings.SaveToDiskEveryMinuteThread.IsAlive) throw new Exception("World save thread died."); //this exception will get displayed by the nice msgbox in release mode
			}
			else //check other things every alternate 2 seconds to spread out the load
			{
				ActiveThreads = (Process.GetCurrentProcess().Threads).OfType<ProcessThread>().Count(t => t.ThreadState == ThreadState.Standby || t.ThreadState == ThreadState.Ready || t.ThreadState == ThreadState.Running);
				TotalThreads = Process.GetCurrentProcess().Threads.Count;
			}

#if DEBUG
			//get all gl errors until errors are clear, this must be done on the main thread to access the GL context
			ErrorCode glErrorCode;
			while ((glErrorCode = GL.GetError()) != ErrorCode.NoError)
			{
				//get red book description for most common error codes
				string glErrorDesc;
				switch (glErrorCode)
				{
					case ErrorCode.InvalidEnum:
						glErrorDesc = "GLenum argument out of range";
						break;
					case ErrorCode.InvalidValue:
						glErrorDesc = "Numeric argument out of range";
						break;
					case ErrorCode.InvalidOperation:
						glErrorDesc = "Operation illegal in current state";
						break;
					case ErrorCode.StackOverflow:
						glErrorDesc = "Command would cause a stack overflow";
						break;
					case ErrorCode.StackUnderflow:
						glErrorDesc = "Command would cause a stack underflow";
						break;
					case ErrorCode.OutOfMemory:
						glErrorDesc = "Not enough memory left to execute command";
						break;
					default:
						glErrorDesc = "(unknown)";
						break;
				}
				Debug.WriteLine("***GL Error! {0}: {1}***", glErrorCode, glErrorDesc);
			}

			Debug.WriteLineIf(_updateTime > 1.03, string.Format("Last update cycle second ran slow. Cycle took {0} seconds", _updateTime));
#endif
		}

		public void Render(FrameEventArgs e)
		{
			_framesRendered++; //increment the frame counter for each rendered frame
		}

		public void Resize(EventArgs e)
		{

		}

		public void Dispose()
		{

		}

		public bool Enabled { get; set; }
	}
}
