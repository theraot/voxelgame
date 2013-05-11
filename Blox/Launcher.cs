using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Hosts.World.Generator;
using Hexpoint.Blox.Utilities;

namespace Hexpoint.Blox
{
	public partial class Launcher : Form
	{
		public Launcher()
		{
			InitializeComponent();
			Settings.Launcher = this;
		}

		private void Launcher_Load(object sender, EventArgs e)
		{
			Config.Load();
			Text = string.Format("{0} {1}", Application.ProductName, Settings.VersionDisplay);
			ddlServerIp.Items.AddRange(new object[] { "127.0.0.1 (localhost)", "hornet.voxelgame.com" });
#if DEBUG
			Text += " (DEBUG)";
#else
			//mnuLaunchServer.Enabled = false; //use this to not show the option to launch a server in release mode

			//auto check for newer version when in release mode
			CheckForUpdates.BeginGetNewestVersionNumber(task => { if (!task.IsFaulted && CheckForUpdates.IsNewerVersion(new Version(task.Result))) btnUpgradeAvailable.Visible = true; });
#endif

			if (Config.Mode == ModeType.JoinServer) rbJoinServer.Checked = true; else rbSinglePlayer.Checked = true; //default to single player
			GameMode_Changed(null, null);
			txtUserName.Text = Config.UserName.Length == 0 ? Environment.MachineName : Config.UserName;

			//server settings
			var serverIndex = ddlServerIp.FindStringExact(Config.Server, 0);
			ddlServerIp.SelectedIndex = (serverIndex < 0 ? 0 : serverIndex);
			txtPort.Text = Config.Port.ToString();

			//world settings
			ddlNewWorldSize.Items.AddRange(new object[] { "4 x 4", "6 x 6", "8 x 8", "12 x 12", "16 x 16", "20 x 20", "24 x 24", "40 x 40", "6 x 12", "12 x 24" });
			if (Environment.Is64BitProcess) ddlNewWorldSize.Items.AddRange(new object[] { "48 x 48", "64 x 64", "96 x 96" });
			LoadWorlds();
			var lastWorldIndex = ddlWorld.FindStringExact(Config.LastWorld);
			ddlWorld.SelectedIndex = (lastWorldIndex < 0 ? 0 : lastWorldIndex); //if the previous world exists default selection to it, otherwise default to creating a new world
			Misc.FillEnumDropDown(ddlNewWorldType, typeof(WorldType), null);
			ddlNewWorldSize.SelectedIndex = ddlNewWorldSize.FindStringExact("12 x 12");

			//video settings
			cbVSync.Checked = Config.VSync;
			cbMipmapping.Checked = Config.Mipmapping;
			cbFog.Checked = Config.Fog;
			cbLinearMagnificationFilter.Checked = Config.LinearMagnificationFilter;
			cbSmoothLighting.Checked = Config.SmoothLighting;
			cbWindowed.Checked = Config.Windowed;
			Misc.FillEnumDropDown(ddlViewDistance, typeof(ViewDistance), ViewDistance.Standard.ToString());
			var viewDistanceIndex = ddlViewDistance.FindStringExact(Config.ViewDistance.ToString());
			ddlViewDistance.SelectedIndex = (viewDistanceIndex < 0 ? 0 : viewDistanceIndex);

			//other settings
			cbSoundEnabled.Checked = Config.SoundEnabled;
			cbMusic.Checked = Config.MusicEnabled;
			cbCreativeMode.Checked = Config.CreativeMode;
		}

		private void LoadWorlds()
		{
			ddlWorld.Items.Clear();
			ddlWorld.Items.Add("<<Create New World>>");
			foreach (var fi in Config.SaveDirectory.GetFiles(string.Format("*{0}", Constants.WORLD_FILE_EXTENSION))) ddlWorld.Items.Add(fi.Name.Replace(Constants.WORLD_FILE_EXTENSION, "")); //create list items for all files with the proper extension
		}

		private void GameMode_Changed(object sender, EventArgs e)
		{
			//radio buttons fire this event when they become checked and unchecked, therefore the event would fire twice for every change
			//this allows the initial setup when the sender is null but ignores the extra event fire we dont care about.
			if (sender != null && !((RadioButton)sender).Checked) return;

			if (rbSinglePlayer.Checked)
			{
				lblWorld.Enabled = true;
				ddlWorld.Enabled = true;
				ddlWorld_SelectedIndexChanged(null, null);
				ddlServerIp.Enabled = false;
				txtPort.Enabled = false;
				txtUserName.Enabled = true;
				gbVideo.Enabled = true;
			}
			else if (rbJoinServer.Checked)
			{
				lblWorld.Enabled = false;
				ddlWorld.Enabled = false;
				ddlWorld_SelectedIndexChanged(null, null);
				ddlServerIp.Enabled = true;
				txtPort.Enabled = true;
				txtUserName.Enabled = true;
				gbVideo.Enabled = true;
			}
		}

		/// <summary>
		/// Create or load a world. Done in the launcher so we can show progress and so the world is ready for the server console when launching a server.
		/// When creating a new world, the name is saved in the config so then either the client or server knows which one to load after loading the config.
		/// Also this way all the world creation parameters dont need to be passed anywhere else.
		/// </summary>
		/// <returns>returns true if there are no errors during the create or load</returns>
		private bool CreateOrLoadWorld()
		{
			if (ddlWorld.SelectedIndex == 0) //creating new world
			{
				string newWorldName = txtNewWorldName.Text.Trim();
				if (newWorldName.Length == 0) { Misc.MessageError("World name is required to create a new world."); return false; }
				if (System.IO.Path.GetInvalidFileNameChars().Any(c => newWorldName.Contains(c.ToString()))) { Misc.MessageError("World name contains an invalid character."); return false; }
				if (Config.SaveDirectory.GetFiles(string.Format("{0}{1}", newWorldName, Constants.WORLD_FILE_EXTENSION)).Length > 0) { Misc.MessageError("World name already exists. A unique name is required."); return false; }

				Settings.WorldFilePath = newWorldName;
				WorldData.WorldType = (WorldType)Enum.Parse(typeof(WorldType), ddlNewWorldType.SelectedItem.ToString());
				WorldData.RawSeed = txtSeed.Text;
				WorldData.GeneratorVersion = Application.ProductVersion;
				var sizes = ddlNewWorldSize.SelectedItem.ToString().Split('x');
				WorldData.SizeInChunksX = int.Parse(sizes[0].Trim());
				WorldData.SizeInChunksZ = int.Parse(sizes[1].Trim());
				WorldData.GenerateWithTrees = cbTrees.Checked;

				UpdateProgress("Generating world...", 0, 0);
				Generator.Generate();
			}
			else //loading previously saved world
			{
				//todo: now this is set in the Load of the server console, should have a better way of having it auto load when config is loaded? come back to this later
				//-ie: this works for client because it gets started in the same process, the server forgets this setting because of starting in a new process
				Settings.WorldFilePath = ddlWorld.SelectedItem.ToString();
				if (!System.IO.File.Exists(Settings.WorldFilePath)) { Misc.MessageError("World not found."); LoadWorlds(); ddlWorld.SelectedIndex = 0; return false; } //user probably deleted/renamed after the ddl loaded, reload the ddl
			}
			return true;
		}

		private void ddlWorld_SelectedIndexChanged(object sender, EventArgs e)
		{
			gbNewWorld.Enabled = (ddlWorld.SelectedIndex == 0 && !rbJoinServer.Checked);
		}

		private IPAddress _serverIp;
		private ushort _serverPort = Server.Controller.TCP_LISTENER_PORT;
		private void btnStart_Click(object sender, EventArgs e)
		{
			if (txtUserName.Enabled && txtUserName.Text.Trim().Length == 0) { Misc.MessageError("UserName is required."); return; }

			FormLoading();

			if (rbJoinServer.Checked)
			{
				try
				{
					var addressList = Dns.GetHostAddresses(ddlServerIp.Text.Split(' ')[0]); //if an ip was entered then no lookup is performed, otherwise a dns lookup is attempted
					foreach (var ipAddress in addressList.Where(ipAddress => ipAddress.GetAddressBytes().Length == 4)) //look for the ipv4 address
					{
						_serverIp = ipAddress;
						break;
					}
					if (_serverIp == null) throw new Exception("Valid IPv4 address not found.");
				}
				catch (Exception ex)
				{
					Misc.MessageError("Invalid Server IP address or hostname: " + ex.Message);
					FormReset();
					return;
				}

				if (!UInt16.TryParse(txtPort.Text, out _serverPort))
				{
					Misc.MessageError("Invalid Server Port.");
					FormReset();
					return;
				}
			}
			else //create or load a world for single player
			{
				if (!CreateOrLoadWorld())
				{
					FormReset();
					return;
				}
			}

			SaveConfig(rbSinglePlayer.Checked ? ModeType.SinglePlayer : ModeType.JoinServer); //save the config (must be done before starting a server thread for single player)

			var backgroundWorker = new BackgroundWorker();
			backgroundWorker.RunWorkerCompleted += InitGame;
			if (rbSinglePlayer.Checked)
			{
				UpdateProgress("Initializing World", 0, 0);
				backgroundWorker.DoWork += delegate { Server.Controller.Launch(); };
			}
			else
			{
				backgroundWorker.DoWork += GameActions.NetworkClient.Connect;
			}
			backgroundWorker.RunWorkerAsync(new object[] { _serverIp, _serverPort });
		}

		private void InitGame(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				if (e.Error != null) throw e.Error;

				UpdateProgress("Initializing Game Window", 0, 0);
				using (var game = new Game())
				{
					Hide(); //need to hide the launcher rather then closing so that any errors can still display in a message box
					Diagnostics.OutputDebugInfo(); //this only works after the game window has been initialized
					game.Icon = Icon; //use this forms icon directly rather then from a resource file so the icon isnt in the output exe twice (reduces exe by 22k)
					if (!Config.Windowed) game.WindowState = OpenTK.WindowState.Fullscreen; else if (Config.Maximized) game.WindowState = OpenTK.WindowState.Maximized;
					game.Run(Constants.UPDATES_PER_SECOND);
				}
			}
			catch (ServerConnectException ex)
			{
				Misc.MessageError(ex.Message);
				FormReset();
				return; //no need to restart for a connect exception
			}
			catch (ServerDisconnectException ex)
			{
#if DEBUG
				Misc.MessageError(string.Format("{0}: {1}", ex.Message, ex.InnerException.StackTrace));
#else
				Misc.MessageError(ex.Message);
#endif
				Application.Restart(); //just restart the app so theres no need to worry about lingering forms, settings, state issues, etc.				
			}
#if DEBUG
#else //comment this line to use this error handling while in debug mode
			catch (Exception ex)
			{
				try
				{
					Misc.MessageError(string.Format("{0}\n\nApplication Version: {1}\nServer: {2}\nPosition: {3}\nPerformance: {4}\n\nOpenGL: {5} {6}\nGLSL: {7}\nVideo Card: {8}\nOS: {9}\nCulture: {10}\n\n{11}",
													ex.Message,
													ProductVersion,
													_serverIp != null ? string.Format("{0}:{1}", _serverIp, _serverPort) : "n/a",
													Game.Player != null ? Game.Player.Coords.ToString() : "unknown",
													Game.PerformanceHost != null ? string.Format("{0} mb, {1} fps", Game.PerformanceHost.Memory, Game.PerformanceHost.Fps) : "unknown",
													Diagnostics.OpenGlVersion,
													Diagnostics.OpenGlVendor,
													Diagnostics.OpenGlGlsl,
													Diagnostics.OpenGlRenderer,
													Diagnostics.OperatingSystem,
													System.Globalization.CultureInfo.CurrentCulture.Name,
													ex.StackTrace));
				}
				catch (Exception exInner)
				{
					//should never happen, but we end up here if while trying to display the nice message some of the info is missing/null
					//and ive already caught myself making several dumb mistakes by having this. its easy to introduce problems with the
					//error handler above because its only getting compiled in release mode, so leave this here as well.
					Misc.MessageError(string.Format("Error: {0}\n\n{1}", exInner.Message, exInner.StackTrace));
				}
			}
#endif
			Application.Exit(); //close the application in case the launcher is still running
		}

		/// <summary>Change the form to a loading state.</summary>
		private void FormLoading()
		{
			foreach (Control control in Controls) control.Enabled = false; //disable the entire form
			btnStart.Text = "Loading...";
		}

		/// <summary>Reset the form if we hit any validation errors.</summary>
		private void FormReset()
		{
			foreach (Control control in Controls) control.Enabled = true;
			btnStart.Text = "Start Game";
			txtProgress.Visible = false;
			pbProgress.Visible = false;
		}

		private void SaveConfig(ModeType mode)
		{
			Config.Mode = mode;
			Config.UserName = txtUserName.Text.Trim();
			Config.Server = ddlServerIp.Text;
			Config.Port = ushort.Parse(txtPort.Text);
			Config.LastWorld = ddlWorld.SelectedIndex == 0 ? txtNewWorldName.Text.Trim() : ddlWorld.SelectedItem.ToString();
			Config.VSync = cbVSync.Checked;
			Config.Mipmapping = cbMipmapping.Checked;
			Config.Fog = cbFog.Checked;
			Config.LinearMagnificationFilter = cbLinearMagnificationFilter.Checked;
			Config.SmoothLighting = cbSmoothLighting.Checked;
			Config.Windowed = cbWindowed.Checked;
			Config.ViewDistance = (ViewDistance)Enum.Parse(typeof(ViewDistance), ddlViewDistance.SelectedItem.ToString());
			Config.SoundEnabled = cbSoundEnabled.Checked;
			Config.MusicEnabled = cbMusic.Checked;
			Config.CreativeMode = cbCreativeMode.Checked && Config.IsSinglePlayer;
			Config.Save();
		}

		internal void UpdateProgressInvokable(string message, int currentProgress, int maxProgress)
		{
			if (!InvokeRequired) UpdateProgress(message, currentProgress, maxProgress); else Invoke((MethodInvoker)(() => UpdateProgress(message, currentProgress, maxProgress))); //allows other threads to update this form
		}

		internal void UpdateProgress(string message, int currentProgress, int maxProgress)
		{
			if (String.IsNullOrEmpty(message))
			{
				txtProgress.Visible = false;
				pbProgress.Visible = false;
			}
			else
			{
				txtProgress.Visible = true;
				txtProgress.Text = message;
				pbProgress.Visible = maxProgress > 0;
				pbProgress.Maximum = maxProgress;
				pbProgress.Value = currentProgress;
			}
			Update();
		}

		#region Menu
		private void mnuLaunchServer_Click(object sender, EventArgs e)
		{
			FormLoading();
			if (!CreateOrLoadWorld())
			{
				FormReset();
				return;
			}

			//this is still going to save all the video settings etc. when you launch a standalone server, but is that something we care about?
			SaveConfig(ModeType.StandaloneServer);

			//start server console in new process
			System.Diagnostics.Process.Start(Application.ExecutablePath, "server");
			FormReset();
		}

		private void mnuExit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void mnuCheckForUpdates_Click(object sender, EventArgs e)
		{
			new CheckForUpdates().ShowDialog(); //modal
		}

		private void mnuVisitWebSite_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start(Constants.URL);
		}

		private void mnuAbout_Click(object sender, EventArgs e)
		{
			MessageBox.Show(string.Format("{0}\n{1}\n{2}\n{3}\n\nSome sounds from:\nwww.soundjay.com\nwww.freesound.org\nwww.soundbible.com\nwww.nosoapradio.us", Application.ProductName, Application.ProductVersion, Constants.URL, Application.CompanyName), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		#endregion
	}
}