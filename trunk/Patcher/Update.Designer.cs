namespace Hexpoint.Patcher
{
	partial class Update
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Update));
			this.webBrowser = new System.Windows.Forms.WebBrowser();
			this.btnOk = new System.Windows.Forms.Button();
			this.lblProgress = new System.Windows.Forms.Label();
			this.imgProgress = new System.Windows.Forms.PictureBox();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			((System.ComponentModel.ISupportInitialize)(this.imgProgress)).BeginInit();
			this.SuspendLayout();
			// 
			// webBrowser
			// 
			this.webBrowser.AllowNavigation = false;
			this.webBrowser.AllowWebBrowserDrop = false;
			this.webBrowser.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.webBrowser.IsWebBrowserContextMenuEnabled = false;
			this.webBrowser.Location = new System.Drawing.Point(0, 94);
			this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
			this.webBrowser.Name = "webBrowser";
			this.webBrowser.ScriptErrorsSuppressed = true;
			this.webBrowser.Size = new System.Drawing.Size(508, 291);
			this.webBrowser.TabIndex = 0;
			this.webBrowser.TabStop = false;
			this.webBrowser.WebBrowserShortcutsEnabled = false;
			// 
			// btnOk
			// 
			this.btnOk.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btnOk.Location = new System.Drawing.Point(211, 53);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(87, 35);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// lblProgress
			// 
			this.lblProgress.AutoSize = true;
			this.lblProgress.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblProgress.Location = new System.Drawing.Point(50, 12);
			this.lblProgress.Name = "lblProgress";
			this.lblProgress.Size = new System.Drawing.Size(226, 20);
			this.lblProgress.TabIndex = 2;
			this.lblProgress.Text = "Downloading new version...";
			// 
			// imgProgress
			// 
			this.imgProgress.Image = global::Hexpoint.Patcher.Properties.Resources.Progress;
			this.imgProgress.Location = new System.Drawing.Point(12, 12);
			this.imgProgress.Name = "imgProgress";
			this.imgProgress.Size = new System.Drawing.Size(32, 32);
			this.imgProgress.TabIndex = 3;
			this.imgProgress.TabStop = false;
			// 
			// progressBar
			// 
			this.progressBar.Location = new System.Drawing.Point(54, 35);
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(442, 10);
			this.progressBar.TabIndex = 4;
			// 
			// Update
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(508, 385);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.imgProgress);
			this.Controls.Add(this.lblProgress);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.webBrowser);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "Update";
			this.Text = "Voxel Game Patcher";
			this.Load += new System.EventHandler(this.Update_Load);
			((System.ComponentModel.ISupportInitialize)(this.imgProgress)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.WebBrowser webBrowser;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Label lblProgress;
		private System.Windows.Forms.PictureBox imgProgress;
		private System.Windows.Forms.ProgressBar progressBar;
	}
}