using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;

namespace Hexpoint.Patcher
{
	/// <summary>
	/// Patcher.exe should reside in the web site download directory.
	/// It only needs to be updated if a change is made to it, for example if patching other files becomes needed.
	/// A file named 'version' should reside in the download directory containing a version number in format 'major.minor' or 'major.minor.build'
	/// Patch notes are loaded from PatchNotes.htm in the download directory.
	/// The most recent obfuscated release VoxelGame.exe should also reside in the download directory.
	/// </summary>
	public partial class Update : Form
	{
		private const string URL = "http://www.voxelgame.com/download";
		private const string EXE_FILE = "VoxelGame.exe";
		private const string UPDATE_FILE = "VoxelGameUpdate.dat";
		private const string PATCH_NOTES_FILE = "PatchNotes.htm";

		public Update()
		{
			InitializeComponent();
		}

		private void Update_Load(object sender, EventArgs e)
		{
#if DEBUG
			//the patcher should only be used in release mode, this will alert us if a debug version is accidentally in use
			MessageBox.Show("Warning: This is a debug version of the patcher.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
#endif
			//steps:
			//1. load patch notes
			//2. download new exe
			//3. replace and archive previous exe
			//4. handle errors
			//5. launch in new process
			//6. exit the patcher
			//7. main process deletes Patcher.exe before launching client or server

			btnOk.Enabled = false;

			//load the patch notes
			//need to use some extra querystring parameters to ensure we dont get cached patch notes from the IE subsystem, also allows us to track a bit who is patching if we want
			webBrowser.Navigate(string.Format("{0}/{1}?id={2}_{3}_{4}", URL, PATCH_NOTES_FILE, System.Globalization.CultureInfo.CurrentCulture.Name, Environment.UserName, DateTime.Now.Ticks));
			
			var wc = new WebClient { Proxy = null }; //setting proxy to null can prevent a 3 second lag in some circumstances such as being connected to a vpn
			wc.DownloadFileCompleted += DownloadFileCompleted;
			wc.DownloadProgressChanged += DownloadProgressChanged;
			var uri = new Uri(string.Format("{0}/{1}", URL, EXE_FILE));
			wc.DownloadFileAsync(uri, UPDATE_FILE); //download to a different file in case theres a problem
		}

		private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			lblProgress.Text = string.Format("Downloaded {0:f0}kb of {1:f0}kb...", e.BytesReceived / 1024, e.TotalBytesToReceive / 1024);
			progressBar.Value = e.ProgressPercentage;
		}

		private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
		{
			progressBar.Value = 100;
			if (e.Error == null)
			{
				lblProgress.Text = "Replacing previous version...";
				try
				{
					//make the switch to the new version and archive the previous one
					System.IO.File.Replace(UPDATE_FILE, EXE_FILE, EXE_FILE + ".bak");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error replacing previous version: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					Application.Exit();
				}
				
				imgProgress.Image = Properties.Resources.GreenCheck;
				btnOk.Enabled = true;
				lblProgress.Text = "New version installed. Click OK to launch.";
			}
			else
			{
				MessageBox.Show("Error downloading new version: " + e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				//the previous version should still be available for them to run if this happens
				Application.Exit();
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				Process.Start(EXE_FILE);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error launching: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			Application.Exit();
		}
	}
}
