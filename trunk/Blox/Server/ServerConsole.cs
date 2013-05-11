using System;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Utilities;

namespace Hexpoint.Blox.Server
{
	internal partial class ServerConsole : Form
	{
		#region Constructors
		public ServerConsole()
		{
			InitializeComponent();
		}

		private const int MAX_LOG_LENGTH = 300;
		private const int MAX_STREAM_LOG_LENGTH = 300;

		private void ServerConsole_Load(object sender, EventArgs e)
		{
			try
			{
				Config.Load(); //config needs to be loaded because the server console starts in a new process
				Config.Mode = ModeType.StandaloneServer; //this would have been saved in the config coming from the launcher, but needs to be set here in case were launching a server in debug or from the command line
				Settings.WorldFilePath = Config.LastWorld; //todo: should be better way to set this, its set here and in the launcher
				Controller.Launch(this);
			}
			catch (Exception ex)
			{
				Misc.MessageError("Error starting server console: " + ex.Message);
				Application.Exit();
			}

			Text = string.Format("Voxel Game Server Console {0}", Application.ProductVersion);
			_startTime = DateTime.Now;
			txtBroadcast.Focus();
			tsWorld.Text = Settings.WorldName;

			//create a datatable to hold player info, used to bind to players grid, easier then using players collection so we dont have to maintain
			//every property on player object we might need. Coords is much easier to leave as a field on player so individual components of it can be updated directly etc.
			_dtPlayers = new DataTable();
			_dtPlayers.Columns.Add("Id", typeof(int));
			_dtPlayers.Columns.Add("UserName", typeof(string));
			_dtPlayers.Columns.Add("Flags", typeof(string));
			_dtPlayers.Columns.Add("ConnectTime", typeof(DateTime));
			_dtPlayers.Columns.Add("IP", typeof(string));
			_dtPlayers.Columns.Add("Location", typeof(string));
			_dtPlayers.Columns.Add("Fps", typeof (short));
			_dtPlayers.Columns.Add("Memory", typeof(short));

			//settings tab
			txtMotd.Text = Config.MOTD;
			txtAdminPassword.Text = Controller.AdminPassword;

			//tools tab
		}
		#endregion

		#region Properties
		private DateTime _startTime;
		private DataTable _dtPlayers;
		#endregion

		internal void UpdateLogInvokable(string message)
		{
			if (!InvokeRequired) UpdateLog(message); else Invoke((MethodInvoker)(() => UpdateLog(message))); //allows other threads to update this form
		}

		private void UpdateLog(string message)
		{
			if (cbIgnoreWorldSaved.Checked && message == Controller.WORLD_SAVED_MESSAGE) return;
			try
			{
				foreach (var line in message.Split('\n'))
				{
					lbLog.Items.Add(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, line));
					if (lbLog.Items.Count > MAX_LOG_LENGTH) lbLog.Items.RemoveAt(0);
					lbLog.TopIndex = lbLog.Items.Count - 1;
				}
			}
			catch (Exception ex)
			{
				Misc.MessageError("Error updating server console log: " + ex.Message);
			}
		}

		internal void UpdatePlayerListInvokable()
		{
			if (!InvokeRequired) UpdatePlayerList(); else Invoke((MethodInvoker)UpdatePlayerList); //allows other threads to update this form
		}

		private void UpdatePlayerList()
		{
			try
			{
				_dtPlayers.Clear();
				foreach (var player in Controller.Players.Values)
				{
					var dr = _dtPlayers.NewRow();
					dr["Id"] = player.Id;
					dr["UserName"] = player.UserName;
					dr["Flags"] = player.FlagsText;
					dr["ConnectTime"] = player.ConnectTime;
					dr["IP"] = player.IpAddress;
					dr["Location"] = player.Coords.ToString();
					dr["Fps"] = player.Fps;
					dr["Memory"] = player.Memory;

					_dtPlayers.Rows.Add(dr);
				}
				gridPlayers.DataSource = _dtPlayers.DefaultView;
				tsPlayersConnected.Text = _dtPlayers.Rows.Count.ToString();
			}
			catch (Exception ex)
			{
				UpdateLog("Error refreshing players grid: " + ex.Message);
			}
		}

		private void txtBroadcast_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode != Keys.Enter) return;
			try
			{
				ServerMsg.Broadcast(txtBroadcast.Text);
				UpdateLog(string.Format("[SERVER] {0}", txtBroadcast.Text));
				txtBroadcast.Clear();
			}
			catch (Exception ex)
			{
				UpdateLog("Error sending broadcast: " + ex.Message);
			}
		}

		#region TCP Stream Tab
		private void cbCaptureOut_CheckedChanged(object sender, EventArgs e)
		{
			Controller.CaptureOutgoing = cbCaptureOut.Checked;
		}
	
		private void cbCaptureIn_CheckedChanged(object sender, EventArgs e)
		{
			Controller.CaptureIncoming = cbCaptureIn.Checked;
			if (cbCaptureIn.Checked)
			{
				cbShowPlayerMove.Enabled = true;
			}
			else
			{
				cbShowPlayerMove.Checked = false;
				cbShowPlayerMove.Enabled = false;
			}
		}

		internal void UpdateStreamLogInvokable(GameAction gameAction, NetworkPlayer player, bool isSending)
		{
			if (!InvokeRequired) UpdateStreamLog(gameAction, player, isSending); else Invoke((MethodInvoker)(() => UpdateStreamLog(gameAction, player, isSending))); //allows other threads to update this form
		}

		private void UpdateStreamLog(GameAction gameAction, NetworkPlayer player, bool isSending)
		{
			//here we can look at the ActionType and decide if we dont want to spam the log, such as for PlayerMove
			if (gameAction.ActionType == ActionType.PlayerMove && !cbShowPlayerMove.Checked) return;
			try
			{
				lbTcpStream.Items.Add(string.Format("{0} [{1}] {2} ({3}b) {4}", isSending ? "SEND" : "RECV", player.Id, player.UserName, gameAction.DataLength, gameAction));
				if (lbTcpStream.Items.Count > MAX_STREAM_LOG_LENGTH) lbTcpStream.Items.RemoveAt(0);
				lbTcpStream.TopIndex = lbTcpStream.Items.Count - 1;
			}
			catch (Exception ex)
			{
				Misc.MessageError("Error updating server console stream: " + ex.Message);
			}
		}
		#endregion

		#region Server Settings Tab
		private void btnMotdUpdate_Click(object sender, EventArgs e)
		{
			txtMotd.Text = txtMotd.Text.Trim();
			Config.MOTD = txtMotd.Text;
			Config.Save();
			ServerMsg.Broadcast(txtMotd.Text);
			UpdateLog(string.Format("[SERVER] {0}", txtMotd.Text));
		}

		private void btnAdminPasswordUpdate_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtAdminPassword.Text)) { Misc.MessageError("Cannot set a blank admin password."); return; }
			Controller.AdminPassword = txtAdminPassword.Text.Trim();
			Misc.MessageInfo("Admin Password updated.");
		}

		private void btnAdminPasswordCopy_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(string.Format("/admin {0}", Controller.AdminPassword));
			Misc.MessageInfo("Admin Password command copied to clipboard.");
		}
		#endregion

		#region Tools Tab
		private void btnReverseIp_Click(object sender, EventArgs e)
		{
			System.Net.IPAddress ip;
			if (System.Net.IPAddress.TryParse(txtIpToReverse.Text, out ip))
				System.Diagnostics.Process.Start(string.Format("http://www.ipchecking.com/?ip={0}&check=Lookup", txtIpToReverse.Text));
			else
				Misc.MessageError("Invalid IP address.");
		}
		#endregion

		#region Menu Choices
		//file menu
		private void mnuNewLauncher_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start(Application.ExecutablePath);
		}

		private void mnuLogClear_Click(object sender, EventArgs e)
		{
			lbLog.Items.Clear();
		}

		private void mnuLogCopyToClipboard_Click(object sender, EventArgs e)
		{
			CopyListBoxToClipboard(lbLog);
		}

		private void mnuTcpStreamClear_Click(object sender, EventArgs e)
		{
			lbTcpStream.Items.Clear();
		}

		private void mnuTcpStreamCopyToClipboard_Click(object sender, EventArgs e)
		{
			CopyListBoxToClipboard(lbTcpStream);
		}

		private static void CopyListBoxToClipboard(ListBox lb)
		{
			var sb = new StringBuilder();
			foreach (var line in lb.Items) sb.AppendLine(line.ToString());
			Clipboard.SetText(sb.ToString());
		}

		private void mnuExit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		//actions menu
		private void mnuRefreshPlayers_Click(object sender, EventArgs e)
		{
			UpdatePlayerList();
		}

		private void mnuSaveWorld_Click(object sender, EventArgs e)
		{
			WorldData.SaveToDisk();
			Controller.WriteToServerConsoleLog(Controller.WORLD_SAVED_MESSAGE);
		}

		private void mnuBackupWorld_Click(object sender, EventArgs e)
		{
			try
			{
				string backupPath = string.Format("{0}_{1:yyyy.MM.dd_HH.mm.ss}.bak", Settings.WorldFilePath, DateTime.Now);
				System.IO.File.Copy(Settings.WorldFilePath, backupPath, true);
				UpdateLog(string.Format("World file backed up to: {0}", backupPath));
			}
			catch (Exception ex)
			{
				Misc.MessageError("Error saving world backup: " + ex.Message);
			}
		}

		//view menu
		private void mnuServerUptime_Click(object sender, EventArgs e)
		{
			var timespan = DateTime.Now - _startTime;
			MessageBox.Show(string.Format("Server start time: {0:MMM dd h:mm tt}\nCurrent Uptime: {1} days {2} hours {3} mins", _startTime, timespan.Days, timespan.Hours, timespan.Minutes), "Server Uptime", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		#endregion

	}
}
