using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hexpoint.Blox.Utilities;

namespace Hexpoint.Blox
{
	public partial class CheckForUpdates : Form
	{
		private const string PATCHER_EXE_FILE = "Patcher.exe";

		public CheckForUpdates()
		{
			InitializeComponent();
		}

		private void CheckForUpdates_Load(object sender, EventArgs e)
		{
			btnOk.Click += (o, args) => Close();
			btnUpdateLater.Click += (o, args) => Close();

			BeginGetNewestVersionNumber(CompareVersions);
		}

		/// <summary>
		/// Begin asynchronously checking the web site for the newest version number.
		/// Expects a file named 'version' at the specified uri containing a version number in format 'major.minor' OR 'major.minor.build'
		/// Called from the launcher in release mode for auto version checking.
		/// </summary>
		/// <param name="continuationAction">Action to continue with when the newest version number is received. Marshals back to the ui thread.</param>
		internal static void BeginGetNewestVersionNumber(Action<Task<string>> continuationAction)
		{
			//the TaskScheduler.FromCurrentSynchronizationContext() forces the continuation to run on the ui thread (invokes are then not required)
			Task<string>.Factory.StartNew(() => new WebClient { Proxy = null }.DownloadString(string.Format("{0}/download/version", Constants.URL))).ContinueWith(continuationAction, TaskScheduler.FromCurrentSynchronizationContext());
		}

		/// <summary>Is the supplied newest version available newer then the locally installed version.</summary>
		internal static bool IsNewerVersion(Version newestVersion)
		{
			var localVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; //use reflection instead of Application.ProductVersion because we want a System.Version type
			return localVersion < newestVersion;
		}

		private void CompareVersions(Task<string> task)
		{
			try
			{
				//check if the task IsFaulted, display all the exceptions using the most specific message possible for each one
				//note: task.Exception cannot be null when the task IsFaulted, but checking anyway to avoid warning
				if (task.IsFaulted && task.Exception != null) throw new Exception(task.Exception.InnerExceptions.Aggregate("", (message, ex) => message + (ex.InnerException != null ? ex.InnerException.Message : ex.Message) + "\n"));

				//task completed ok
				var newestVersion = new Version(task.Result);
				if (IsNewerVersion(newestVersion))
				{
					//there is a new version available
					imgProgress.Image = Properties.Resources.GreenFlag;
					lblProgress.Text = string.Format("Version {0} is available for download.", newestVersion);
					btnUpdateLater.Visible = true;
					btnUpdateNow.Visible = true;
					AcceptButton = btnUpdateNow;
				}
				else
				{
					//already running current version
					imgProgress.Image = Properties.Resources.GreenCheck;
					lblProgress.Text = "You have the latest available version.";
					btnOk.Visible = true;
					AcceptButton = btnOk;
				}
			}
			catch (Exception ex)
			{
				Misc.MessageError("Error checking for updates: " + ex.Message);
				Close();
			}
		}

		private void btnUpdateNow_Click(object sender, EventArgs e)
		{
			//steps:
			//1. download Patcher.exe
			//2. launch it in new process
			//3. close this app
			//4. patcher downloads new update, archives previous .exe and replaces it
			//5. patcher starts updated .exe in new process and exits
			//6. main process deletes Patcher.exe

			btnUpdateLater.Enabled = false;
			btnUpdateNow.Enabled = false;
			imgProgress.Image = Properties.Resources.Progress;
			lblProgress.Text = "Downloading Patcher...";
			var wc = new WebClient { Proxy = null }; //setting proxy to null can prevent a 3 second lag in some circumstances such as being connected to a vpn
			wc.DownloadFileCompleted += DownloadFileCompleted;
			var uri = new Uri(string.Format("{0}/download/{1}", Constants.URL, PATCHER_EXE_FILE));
			wc.DownloadFileAsync(uri, PATCHER_EXE_FILE);
		}

		private void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			imgProgress.Enabled = false; //stop spinning
			if (e.Error == null)
			{
				try
				{
					System.Diagnostics.Process.Start(PATCHER_EXE_FILE);
					Application.Exit();
				}
				catch (Exception ex)
				{
					Misc.MessageError("Error launching Patcher: " + ex.Message);
					Close();
				}
			}
			else
			{
				Misc.MessageError("Error downloading Patcher: " + e.Error.Message);
				Close();
			}
		}
	}
}
