namespace Hexpoint.Blox
{
	partial class CheckForUpdates
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
			this.imgProgress = new System.Windows.Forms.PictureBox();
			this.lblProgress = new System.Windows.Forms.Label();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnUpdateNow = new System.Windows.Forms.Button();
			this.btnUpdateLater = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.imgProgress)).BeginInit();
			this.SuspendLayout();
			// 
			// imgProgress
			// 
			this.imgProgress.Image = global::Hexpoint.Blox.Properties.Resources.Progress;
			this.imgProgress.Location = new System.Drawing.Point(12, 9);
			this.imgProgress.Name = "imgProgress";
			this.imgProgress.Size = new System.Drawing.Size(32, 32);
			this.imgProgress.TabIndex = 0;
			this.imgProgress.TabStop = false;
			// 
			// lblProgress
			// 
			this.lblProgress.AutoSize = true;
			this.lblProgress.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblProgress.Location = new System.Drawing.Point(51, 9);
			this.lblProgress.Name = "lblProgress";
			this.lblProgress.Size = new System.Drawing.Size(237, 20);
			this.lblProgress.TabIndex = 1;
			this.lblProgress.Text = "Checking for latest version...";
			this.lblProgress.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// btnOk
			// 
			this.btnOk.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btnOk.Location = new System.Drawing.Point(168, 56);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 31);
			this.btnOk.TabIndex = 2;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Visible = false;
			// 
			// btnUpdateNow
			// 
			this.btnUpdateNow.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btnUpdateNow.Location = new System.Drawing.Point(206, 56);
			this.btnUpdateNow.Name = "btnUpdateNow";
			this.btnUpdateNow.Size = new System.Drawing.Size(126, 31);
			this.btnUpdateNow.TabIndex = 3;
			this.btnUpdateNow.Text = "Update Now";
			this.btnUpdateNow.UseVisualStyleBackColor = true;
			this.btnUpdateNow.Visible = false;
			this.btnUpdateNow.Click += new System.EventHandler(this.btnUpdateNow_Click);
			// 
			// btnUpdateLater
			// 
			this.btnUpdateLater.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btnUpdateLater.Location = new System.Drawing.Point(78, 56);
			this.btnUpdateLater.Name = "btnUpdateLater";
			this.btnUpdateLater.Size = new System.Drawing.Size(122, 31);
			this.btnUpdateLater.TabIndex = 4;
			this.btnUpdateLater.Text = "Update Later";
			this.btnUpdateLater.UseVisualStyleBackColor = true;
			this.btnUpdateLater.Visible = false;
			// 
			// CheckForUpdates
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(411, 95);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.btnUpdateLater);
			this.Controls.Add(this.btnUpdateNow);
			this.Controls.Add(this.lblProgress);
			this.Controls.Add(this.imgProgress);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "CheckForUpdates";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Check For Updates";
			this.Load += new System.EventHandler(this.CheckForUpdates_Load);
			((System.ComponentModel.ISupportInitialize)(this.imgProgress)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox imgProgress;
		private System.Windows.Forms.Label lblProgress;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnUpdateNow;
		private System.Windows.Forms.Button btnUpdateLater;
	}
}