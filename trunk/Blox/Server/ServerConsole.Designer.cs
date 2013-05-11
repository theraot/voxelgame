using System.ComponentModel;
using System.Windows.Forms;

namespace Hexpoint.Blox.Server
{
	partial class ServerConsole
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerConsole));
			this.txtBroadcast = new System.Windows.Forms.TextBox();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.tsPlayersConnected = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.tsWorld = new System.Windows.Forms.ToolStripStatusLabel();
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuNewLauncher = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuLog = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuLogClear = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuLogCopyToClipboard = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuTcpStream = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuTcpStreamClear = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuTcpStreamCopyToClipboard = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuActions = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuSaveWorld = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuBackupWorld = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuView = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuRefreshPlayers = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuServerUptime = new System.Windows.Forms.ToolStripMenuItem();
			this.gridPlayers = new System.Windows.Forms.DataGridView();
			this.colId = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colUserName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colFlags = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colConnectTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colIp = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colLocation = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colFps = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.lbLog = new System.Windows.Forms.ListBox();
			this.cbIgnoreWorldSaved = new System.Windows.Forms.CheckBox();
			this.lblBroadcast = new System.Windows.Forms.Label();
			this.tabContainer = new System.Windows.Forms.TabControl();
			this.tabTcpStream = new System.Windows.Forms.TabPage();
			this.cbCaptureOut = new System.Windows.Forms.CheckBox();
			this.cbShowPlayerMove = new System.Windows.Forms.CheckBox();
			this.lbTcpStream = new System.Windows.Forms.ListBox();
			this.cbCaptureIn = new System.Windows.Forms.CheckBox();
			this.tabServerSettings = new System.Windows.Forms.TabPage();
			this.btnAdminPasswordCopy = new System.Windows.Forms.Button();
			this.btnAdminPasswordUpdate = new System.Windows.Forms.Button();
			this.txtAdminPassword = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnMotdUpdate = new System.Windows.Forms.Button();
			this.txtMotd = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.tabTools = new System.Windows.Forms.TabPage();
			this.btnReverseIp = new System.Windows.Forms.Button();
			this.txtIpToReverse = new System.Windows.Forms.TextBox();
			this.statusStrip.SuspendLayout();
			this.menuStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.gridPlayers)).BeginInit();
			this.tabContainer.SuspendLayout();
			this.tabTcpStream.SuspendLayout();
			this.tabServerSettings.SuspendLayout();
			this.tabTools.SuspendLayout();
			this.SuspendLayout();
			// 
			// txtBroadcast
			// 
			this.txtBroadcast.Location = new System.Drawing.Point(107, 201);
			this.txtBroadcast.Name = "txtBroadcast";
			this.txtBroadcast.Size = new System.Drawing.Size(620, 20);
			this.txtBroadcast.TabIndex = 1;
			this.txtBroadcast.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtBroadcast_KeyDown);
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.tsPlayersConnected,
            this.toolStripStatusLabel2,
            this.tsWorld});
			this.statusStrip.Location = new System.Drawing.Point(0, 576);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(1254, 24);
			this.statusStrip.TabIndex = 9;
			this.statusStrip.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(105, 19);
			this.toolStripStatusLabel1.Text = "Players Connected";
			// 
			// tsPlayersConnected
			// 
			this.tsPlayersConnected.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.tsPlayersConnected.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
			this.tsPlayersConnected.Name = "tsPlayersConnected";
			this.tsPlayersConnected.Size = new System.Drawing.Size(17, 19);
			this.tsPlayersConnected.Text = "0";
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(39, 19);
			this.toolStripStatusLabel2.Text = "World";
			// 
			// tsWorld
			// 
			this.tsWorld.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.tsWorld.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
			this.tsWorld.Name = "tsWorld";
			this.tsWorld.Size = new System.Drawing.Size(16, 19);
			this.tsWorld.Text = "?";
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile,
            this.mnuActions,
            this.mnuView});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size(1254, 24);
			this.menuStrip.TabIndex = 10;
			this.menuStrip.Text = "menuStrip1";
			// 
			// mnuFile
			// 
			this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuNewLauncher,
            this.toolStripMenuItem1,
            this.mnuLog,
            this.mnuTcpStream,
            this.toolStripMenuItem2,
            this.mnuExit});
			this.mnuFile.Name = "mnuFile";
			this.mnuFile.Size = new System.Drawing.Size(37, 20);
			this.mnuFile.Text = "File";
			// 
			// mnuNewLauncher
			// 
			this.mnuNewLauncher.Name = "mnuNewLauncher";
			this.mnuNewLauncher.Size = new System.Drawing.Size(150, 22);
			this.mnuNewLauncher.Text = "New Launcher";
			this.mnuNewLauncher.Click += new System.EventHandler(this.mnuNewLauncher_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(147, 6);
			// 
			// mnuLog
			// 
			this.mnuLog.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuLogClear,
            this.mnuLogCopyToClipboard});
			this.mnuLog.Name = "mnuLog";
			this.mnuLog.Size = new System.Drawing.Size(150, 22);
			this.mnuLog.Text = "Log";
			// 
			// mnuLogClear
			// 
			this.mnuLogClear.Name = "mnuLogClear";
			this.mnuLogClear.Size = new System.Drawing.Size(171, 22);
			this.mnuLogClear.Text = "Clear";
			this.mnuLogClear.Click += new System.EventHandler(this.mnuLogClear_Click);
			// 
			// mnuLogCopyToClipboard
			// 
			this.mnuLogCopyToClipboard.Name = "mnuLogCopyToClipboard";
			this.mnuLogCopyToClipboard.Size = new System.Drawing.Size(171, 22);
			this.mnuLogCopyToClipboard.Text = "Copy to Clipboard";
			this.mnuLogCopyToClipboard.Click += new System.EventHandler(this.mnuLogCopyToClipboard_Click);
			// 
			// mnuTcpStream
			// 
			this.mnuTcpStream.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuTcpStreamClear,
            this.mnuTcpStreamCopyToClipboard});
			this.mnuTcpStream.Name = "mnuTcpStream";
			this.mnuTcpStream.Size = new System.Drawing.Size(150, 22);
			this.mnuTcpStream.Text = "TCP Stream";
			// 
			// mnuTcpStreamClear
			// 
			this.mnuTcpStreamClear.Name = "mnuTcpStreamClear";
			this.mnuTcpStreamClear.Size = new System.Drawing.Size(171, 22);
			this.mnuTcpStreamClear.Text = "Clear";
			this.mnuTcpStreamClear.Click += new System.EventHandler(this.mnuTcpStreamClear_Click);
			// 
			// mnuTcpStreamCopyToClipboard
			// 
			this.mnuTcpStreamCopyToClipboard.Name = "mnuTcpStreamCopyToClipboard";
			this.mnuTcpStreamCopyToClipboard.Size = new System.Drawing.Size(171, 22);
			this.mnuTcpStreamCopyToClipboard.Text = "Copy to Clipboard";
			this.mnuTcpStreamCopyToClipboard.Click += new System.EventHandler(this.mnuTcpStreamCopyToClipboard_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(147, 6);
			// 
			// mnuExit
			// 
			this.mnuExit.Name = "mnuExit";
			this.mnuExit.Size = new System.Drawing.Size(150, 22);
			this.mnuExit.Text = "Exit";
			this.mnuExit.Click += new System.EventHandler(this.mnuExit_Click);
			// 
			// mnuActions
			// 
			this.mnuActions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuSaveWorld,
            this.mnuBackupWorld});
			this.mnuActions.Name = "mnuActions";
			this.mnuActions.Size = new System.Drawing.Size(59, 20);
			this.mnuActions.Text = "Actions";
			// 
			// mnuSaveWorld
			// 
			this.mnuSaveWorld.Name = "mnuSaveWorld";
			this.mnuSaveWorld.Size = new System.Drawing.Size(148, 22);
			this.mnuSaveWorld.Text = "Save World";
			this.mnuSaveWorld.Click += new System.EventHandler(this.mnuSaveWorld_Click);
			// 
			// mnuBackupWorld
			// 
			this.mnuBackupWorld.Name = "mnuBackupWorld";
			this.mnuBackupWorld.Size = new System.Drawing.Size(148, 22);
			this.mnuBackupWorld.Text = "Backup World";
			this.mnuBackupWorld.Click += new System.EventHandler(this.mnuBackupWorld_Click);
			// 
			// mnuView
			// 
			this.mnuView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuRefreshPlayers,
            this.toolStripMenuItem3,
            this.mnuServerUptime});
			this.mnuView.Name = "mnuView";
			this.mnuView.Size = new System.Drawing.Size(44, 20);
			this.mnuView.Text = "View";
			// 
			// mnuRefreshPlayers
			// 
			this.mnuRefreshPlayers.Name = "mnuRefreshPlayers";
			this.mnuRefreshPlayers.Size = new System.Drawing.Size(153, 22);
			this.mnuRefreshPlayers.Text = "Refresh Players";
			this.mnuRefreshPlayers.Click += new System.EventHandler(this.mnuRefreshPlayers_Click);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(150, 6);
			// 
			// mnuServerUptime
			// 
			this.mnuServerUptime.Name = "mnuServerUptime";
			this.mnuServerUptime.Size = new System.Drawing.Size(153, 22);
			this.mnuServerUptime.Text = "Server Uptime";
			this.mnuServerUptime.Click += new System.EventHandler(this.mnuServerUptime_Click);
			// 
			// gridPlayers
			// 
			this.gridPlayers.AllowUserToAddRows = false;
			this.gridPlayers.AllowUserToDeleteRows = false;
			this.gridPlayers.AllowUserToResizeRows = false;
			this.gridPlayers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridPlayers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colId,
            this.colUserName,
            this.colFlags,
            this.colConnectTime,
            this.colIp,
            this.colLocation,
            this.colFps,
            this.colMemory});
			this.gridPlayers.Location = new System.Drawing.Point(12, 27);
			this.gridPlayers.Name = "gridPlayers";
			this.gridPlayers.ReadOnly = true;
			this.gridPlayers.Size = new System.Drawing.Size(715, 168);
			this.gridPlayers.TabIndex = 11;
			// 
			// colId
			// 
			this.colId.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.colId.DataPropertyName = "Id";
			this.colId.HeaderText = "ID";
			this.colId.Name = "colId";
			this.colId.ReadOnly = true;
			this.colId.Width = 30;
			// 
			// colUserName
			// 
			this.colUserName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.colUserName.DataPropertyName = "UserName";
			this.colUserName.HeaderText = "UserName";
			this.colUserName.Name = "colUserName";
			this.colUserName.ReadOnly = true;
			// 
			// colFlags
			// 
			this.colFlags.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.colFlags.DataPropertyName = "Flags";
			this.colFlags.HeaderText = "Flags";
			this.colFlags.Name = "colFlags";
			this.colFlags.ReadOnly = true;
			this.colFlags.Width = 40;
			// 
			// colConnectTime
			// 
			this.colConnectTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.colConnectTime.DataPropertyName = "ConnectTime";
			dataGridViewCellStyle1.Format = "MMM dd h:mm:ss tt";
			this.colConnectTime.DefaultCellStyle = dataGridViewCellStyle1;
			this.colConnectTime.HeaderText = "Connect Time";
			this.colConnectTime.Name = "colConnectTime";
			this.colConnectTime.ReadOnly = true;
			this.colConnectTime.Width = 98;
			// 
			// colIp
			// 
			this.colIp.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.colIp.DataPropertyName = "Ip";
			this.colIp.HeaderText = "IP";
			this.colIp.Name = "colIp";
			this.colIp.ReadOnly = true;
			// 
			// colLocation
			// 
			this.colLocation.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.colLocation.DataPropertyName = "Location";
			this.colLocation.HeaderText = "Location";
			this.colLocation.Name = "colLocation";
			this.colLocation.ReadOnly = true;
			// 
			// colFps
			// 
			this.colFps.DataPropertyName = "Fps";
			this.colFps.HeaderText = "FPS";
			this.colFps.Name = "colFps";
			this.colFps.ReadOnly = true;
			this.colFps.Width = 50;
			// 
			// colMemory
			// 
			this.colMemory.DataPropertyName = "Memory";
			this.colMemory.HeaderText = "Memory";
			this.colMemory.Name = "colMemory";
			this.colMemory.ReadOnly = true;
			this.colMemory.Width = 50;
			// 
			// lbLog
			// 
			this.lbLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.lbLog.FormattingEnabled = true;
			this.lbLog.HorizontalScrollbar = true;
			this.lbLog.Location = new System.Drawing.Point(12, 227);
			this.lbLog.Name = "lbLog";
			this.lbLog.Size = new System.Drawing.Size(715, 342);
			this.lbLog.TabIndex = 13;
			// 
			// cbIgnoreWorldSaved
			// 
			this.cbIgnoreWorldSaved.AutoSize = true;
			this.cbIgnoreWorldSaved.Checked = true;
			this.cbIgnoreWorldSaved.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbIgnoreWorldSaved.Location = new System.Drawing.Point(9, 118);
			this.cbIgnoreWorldSaved.Name = "cbIgnoreWorldSaved";
			this.cbIgnoreWorldSaved.Size = new System.Drawing.Size(224, 17);
			this.cbIgnoreWorldSaved.TabIndex = 14;
			this.cbIgnoreWorldSaved.Text = "Log should ignore \'World Saved\' message";
			this.cbIgnoreWorldSaved.UseVisualStyleBackColor = true;
			// 
			// lblBroadcast
			// 
			this.lblBroadcast.AutoSize = true;
			this.lblBroadcast.Location = new System.Drawing.Point(12, 204);
			this.lblBroadcast.Name = "lblBroadcast";
			this.lblBroadcast.Size = new System.Drawing.Size(89, 13);
			this.lblBroadcast.TabIndex = 15;
			this.lblBroadcast.Text = "Server Broadcast";
			// 
			// tabContainer
			// 
			this.tabContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabContainer.Controls.Add(this.tabTcpStream);
			this.tabContainer.Controls.Add(this.tabServerSettings);
			this.tabContainer.Controls.Add(this.tabTools);
			this.tabContainer.Location = new System.Drawing.Point(733, 27);
			this.tabContainer.Name = "tabContainer";
			this.tabContainer.SelectedIndex = 0;
			this.tabContainer.Size = new System.Drawing.Size(509, 546);
			this.tabContainer.TabIndex = 18;
			// 
			// tabTcpStream
			// 
			this.tabTcpStream.Controls.Add(this.cbCaptureOut);
			this.tabTcpStream.Controls.Add(this.cbShowPlayerMove);
			this.tabTcpStream.Controls.Add(this.lbTcpStream);
			this.tabTcpStream.Controls.Add(this.cbCaptureIn);
			this.tabTcpStream.Location = new System.Drawing.Point(4, 22);
			this.tabTcpStream.Name = "tabTcpStream";
			this.tabTcpStream.Padding = new System.Windows.Forms.Padding(3);
			this.tabTcpStream.Size = new System.Drawing.Size(501, 520);
			this.tabTcpStream.TabIndex = 0;
			this.tabTcpStream.Text = "TCP Stream";
			this.tabTcpStream.UseVisualStyleBackColor = true;
			// 
			// cbCaptureOut
			// 
			this.cbCaptureOut.AutoSize = true;
			this.cbCaptureOut.Location = new System.Drawing.Point(6, 6);
			this.cbCaptureOut.Name = "cbCaptureOut";
			this.cbCaptureOut.Size = new System.Drawing.Size(109, 17);
			this.cbCaptureOut.TabIndex = 22;
			this.cbCaptureOut.Text = "Capture Outgoing";
			this.cbCaptureOut.UseVisualStyleBackColor = true;
			this.cbCaptureOut.CheckedChanged += new System.EventHandler(this.cbCaptureOut_CheckedChanged);
			// 
			// cbShowPlayerMove
			// 
			this.cbShowPlayerMove.AutoSize = true;
			this.cbShowPlayerMove.Enabled = false;
			this.cbShowPlayerMove.Location = new System.Drawing.Point(236, 6);
			this.cbShowPlayerMove.Name = "cbShowPlayerMove";
			this.cbShowPlayerMove.Size = new System.Drawing.Size(115, 17);
			this.cbShowPlayerMove.TabIndex = 21;
			this.cbShowPlayerMove.Text = "Show Player Move";
			this.cbShowPlayerMove.UseVisualStyleBackColor = true;
			// 
			// lbTcpStream
			// 
			this.lbTcpStream.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lbTcpStream.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbTcpStream.FormattingEnabled = true;
			this.lbTcpStream.HorizontalScrollbar = true;
			this.lbTcpStream.ItemHeight = 12;
			this.lbTcpStream.Location = new System.Drawing.Point(3, 28);
			this.lbTcpStream.Name = "lbTcpStream";
			this.lbTcpStream.Size = new System.Drawing.Size(495, 484);
			this.lbTcpStream.TabIndex = 20;
			// 
			// cbCaptureIn
			// 
			this.cbCaptureIn.AutoSize = true;
			this.cbCaptureIn.Location = new System.Drawing.Point(121, 6);
			this.cbCaptureIn.Name = "cbCaptureIn";
			this.cbCaptureIn.Size = new System.Drawing.Size(109, 17);
			this.cbCaptureIn.TabIndex = 19;
			this.cbCaptureIn.Text = "Capture Incoming";
			this.cbCaptureIn.UseVisualStyleBackColor = true;
			this.cbCaptureIn.CheckedChanged += new System.EventHandler(this.cbCaptureIn_CheckedChanged);
			// 
			// tabServerSettings
			// 
			this.tabServerSettings.Controls.Add(this.btnAdminPasswordCopy);
			this.tabServerSettings.Controls.Add(this.btnAdminPasswordUpdate);
			this.tabServerSettings.Controls.Add(this.txtAdminPassword);
			this.tabServerSettings.Controls.Add(this.label2);
			this.tabServerSettings.Controls.Add(this.btnMotdUpdate);
			this.tabServerSettings.Controls.Add(this.txtMotd);
			this.tabServerSettings.Controls.Add(this.cbIgnoreWorldSaved);
			this.tabServerSettings.Controls.Add(this.label1);
			this.tabServerSettings.Location = new System.Drawing.Point(4, 22);
			this.tabServerSettings.Name = "tabServerSettings";
			this.tabServerSettings.Padding = new System.Windows.Forms.Padding(3);
			this.tabServerSettings.Size = new System.Drawing.Size(501, 520);
			this.tabServerSettings.TabIndex = 1;
			this.tabServerSettings.Text = "Server Settings";
			this.tabServerSettings.UseVisualStyleBackColor = true;
			// 
			// btnAdminPasswordCopy
			// 
			this.btnAdminPasswordCopy.Location = new System.Drawing.Point(174, 79);
			this.btnAdminPasswordCopy.Name = "btnAdminPasswordCopy";
			this.btnAdminPasswordCopy.Size = new System.Drawing.Size(75, 23);
			this.btnAdminPasswordCopy.TabIndex = 18;
			this.btnAdminPasswordCopy.Text = "Copy";
			this.btnAdminPasswordCopy.UseVisualStyleBackColor = true;
			this.btnAdminPasswordCopy.Click += new System.EventHandler(this.btnAdminPasswordCopy_Click);
			// 
			// btnAdminPasswordUpdate
			// 
			this.btnAdminPasswordUpdate.Location = new System.Drawing.Point(93, 79);
			this.btnAdminPasswordUpdate.Name = "btnAdminPasswordUpdate";
			this.btnAdminPasswordUpdate.Size = new System.Drawing.Size(75, 23);
			this.btnAdminPasswordUpdate.TabIndex = 17;
			this.btnAdminPasswordUpdate.Text = "Update";
			this.btnAdminPasswordUpdate.UseVisualStyleBackColor = true;
			this.btnAdminPasswordUpdate.Click += new System.EventHandler(this.btnAdminPasswordUpdate_Click);
			// 
			// txtAdminPassword
			// 
			this.txtAdminPassword.Location = new System.Drawing.Point(9, 81);
			this.txtAdminPassword.MaxLength = 8;
			this.txtAdminPassword.Name = "txtAdminPassword";
			this.txtAdminPassword.Size = new System.Drawing.Size(78, 20);
			this.txtAdminPassword.TabIndex = 16;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 65);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(85, 13);
			this.label2.TabIndex = 15;
			this.label2.Text = "Admin Password";
			// 
			// btnMotdUpdate
			// 
			this.btnMotdUpdate.Location = new System.Drawing.Point(420, 26);
			this.btnMotdUpdate.Name = "btnMotdUpdate";
			this.btnMotdUpdate.Size = new System.Drawing.Size(75, 23);
			this.btnMotdUpdate.TabIndex = 5;
			this.btnMotdUpdate.Text = "Update";
			this.btnMotdUpdate.UseVisualStyleBackColor = true;
			this.btnMotdUpdate.Click += new System.EventHandler(this.btnMotdUpdate_Click);
			// 
			// txtMotd
			// 
			this.txtMotd.Location = new System.Drawing.Point(9, 29);
			this.txtMotd.MaxLength = 80;
			this.txtMotd.Name = "txtMotd";
			this.txtMotd.Size = new System.Drawing.Size(405, 20);
			this.txtMotd.TabIndex = 4;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(102, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Message of the Day";
			// 
			// tabTools
			// 
			this.tabTools.Controls.Add(this.btnReverseIp);
			this.tabTools.Controls.Add(this.txtIpToReverse);
			this.tabTools.Location = new System.Drawing.Point(4, 22);
			this.tabTools.Name = "tabTools";
			this.tabTools.Padding = new System.Windows.Forms.Padding(3);
			this.tabTools.Size = new System.Drawing.Size(501, 520);
			this.tabTools.TabIndex = 2;
			this.tabTools.Text = "Tools";
			this.tabTools.UseVisualStyleBackColor = true;
			// 
			// btnReverseIp
			// 
			this.btnReverseIp.Location = new System.Drawing.Point(117, 6);
			this.btnReverseIp.Name = "btnReverseIp";
			this.btnReverseIp.Size = new System.Drawing.Size(75, 23);
			this.btnReverseIp.TabIndex = 1;
			this.btnReverseIp.Text = "Reverse IP";
			this.btnReverseIp.UseVisualStyleBackColor = true;
			this.btnReverseIp.Click += new System.EventHandler(this.btnReverseIp_Click);
			// 
			// txtIpToReverse
			// 
			this.txtIpToReverse.Location = new System.Drawing.Point(9, 8);
			this.txtIpToReverse.MaxLength = 15;
			this.txtIpToReverse.Name = "txtIpToReverse";
			this.txtIpToReverse.Size = new System.Drawing.Size(102, 20);
			this.txtIpToReverse.TabIndex = 0;
			// 
			// ServerConsole
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1254, 600);
			this.Controls.Add(this.tabContainer);
			this.Controls.Add(this.lblBroadcast);
			this.Controls.Add(this.lbLog);
			this.Controls.Add(this.gridPlayers);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.menuStrip);
			this.Controls.Add(this.txtBroadcast);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip;
			this.MinimumSize = new System.Drawing.Size(1270, 400);
			this.Name = "ServerConsole";
			this.Text = "Voxel Game Server Console";
			this.Load += new System.EventHandler(this.ServerConsole_Load);
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.gridPlayers)).EndInit();
			this.tabContainer.ResumeLayout(false);
			this.tabTcpStream.ResumeLayout(false);
			this.tabTcpStream.PerformLayout();
			this.tabServerSettings.ResumeLayout(false);
			this.tabServerSettings.PerformLayout();
			this.tabTools.ResumeLayout(false);
			this.tabTools.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox txtBroadcast;
		private StatusStrip statusStrip;
		private MenuStrip menuStrip;
		private ToolStripMenuItem mnuFile;
		private ToolStripMenuItem mnuExit;
		private ToolStripMenuItem mnuActions;
		private ToolStripMenuItem mnuSaveWorld;
		private DataGridView gridPlayers;
		private ToolStripStatusLabel tsPlayersConnected;
		private ToolStripStatusLabel tsWorld;
		private ToolStripStatusLabel toolStripStatusLabel1;
		private ToolStripStatusLabel toolStripStatusLabel2;
		private ListBox lbLog;
		private CheckBox cbIgnoreWorldSaved;
		private Label lblBroadcast;
		private ToolStripMenuItem mnuView;
		private ToolStripMenuItem mnuServerUptime;
		private ToolStripMenuItem mnuNewLauncher;
		private DataGridViewTextBoxColumn colId;
		private DataGridViewTextBoxColumn colUserName;
		private DataGridViewTextBoxColumn colFlags;
		private DataGridViewTextBoxColumn colConnectTime;
		private DataGridViewTextBoxColumn colIp;
		private DataGridViewTextBoxColumn colLocation;
		private DataGridViewTextBoxColumn colFps;
		private DataGridViewTextBoxColumn colMemory;
		private ToolStripSeparator toolStripMenuItem1;
		private ToolStripMenuItem mnuBackupWorld;
		private TabControl tabContainer;
		private TabPage tabTcpStream;
		private CheckBox cbCaptureOut;
		private CheckBox cbShowPlayerMove;
		private ListBox lbTcpStream;
		private CheckBox cbCaptureIn;
		private TabPage tabServerSettings;
		private TabPage tabTools;
		private Button btnMotdUpdate;
		private TextBox txtMotd;
		private Label label1;
		private TextBox txtIpToReverse;
		private Button btnReverseIp;
		private ToolStripMenuItem mnuRefreshPlayers;
		private ToolStripSeparator toolStripMenuItem3;
		private ToolStripMenuItem mnuLog;
		private ToolStripMenuItem mnuLogClear;
		private ToolStripMenuItem mnuLogCopyToClipboard;
		private ToolStripMenuItem mnuTcpStream;
		private ToolStripMenuItem mnuTcpStreamClear;
		private ToolStripMenuItem mnuTcpStreamCopyToClipboard;
		private ToolStripSeparator toolStripMenuItem2;
		private Button btnAdminPasswordUpdate;
		private TextBox txtAdminPassword;
		private Label label2;
		private Button btnAdminPasswordCopy;
	}
}

