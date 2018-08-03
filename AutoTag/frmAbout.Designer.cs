namespace AutoTag {
	partial class frmAbout {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAbout));
			this.lblSources = new System.Windows.Forms.Label();
			this.pBoxLogo = new System.Windows.Forms.PictureBox();
			this.lblChangelog = new System.Windows.Forms.Label();
			this.lblVer = new System.Windows.Forms.Label();
			this.lblTitle = new System.Windows.Forms.Label();
			this.lnkLblTvDbSharper = new System.Windows.Forms.LinkLabel();
			this.lnkLblSubtitleFetcher = new System.Windows.Forms.LinkLabel();
			this.lnkLblTaglib = new System.Windows.Forms.LinkLabel();
			this.lblSubtitleFetcher = new System.Windows.Forms.Label();
			this.lblTaglib = new System.Windows.Forms.Label();
			this.lblTvDbSharper = new System.Windows.Forms.Label();
			this.lnkLblSource = new System.Windows.Forms.LinkLabel();
			this.lnkLblWebsite = new System.Windows.Forms.LinkLabel();
			this.lblThanks = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pBoxLogo)).BeginInit();
			this.SuspendLayout();
			// 
			// lblSources
			// 
			this.lblSources.AutoSize = true;
			this.lblSources.Location = new System.Drawing.Point(12, 139);
			this.lblSources.Margin = new System.Windows.Forms.Padding(3, 0, 3, 5);
			this.lblSources.Name = "lblSources";
			this.lblSources.Size = new System.Drawing.Size(309, 13);
			this.lblSources.TabIndex = 38;
			this.lblSources.Text = "AutoTag utilises the following open-source projects in part or full:\r\n";
			// 
			// pBoxLogo
			// 
			this.pBoxLogo.Cursor = System.Windows.Forms.Cursors.Default;
			this.pBoxLogo.Image = ((System.Drawing.Image)(resources.GetObject("pBoxLogo.Image")));
			this.pBoxLogo.InitialImage = null;
			this.pBoxLogo.Location = new System.Drawing.Point(266, 13);
			this.pBoxLogo.Name = "pBoxLogo";
			this.pBoxLogo.Size = new System.Drawing.Size(110, 110);
			this.pBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pBoxLogo.TabIndex = 36;
			this.pBoxLogo.TabStop = false;
			// 
			// lblChangelog
			// 
			this.lblChangelog.AutoSize = true;
			this.lblChangelog.Location = new System.Drawing.Point(12, 85);
			this.lblChangelog.MaximumSize = new System.Drawing.Size(250, 0);
			this.lblChangelog.Name = "lblChangelog";
			this.lblChangelog.Size = new System.Drawing.Size(174, 26);
			this.lblChangelog.TabIndex = 35;
			this.lblChangelog.Text = "Changelog:\r\nFix cover art tagging with some files";
			// 
			// lblVer
			// 
			this.lblVer.AutoSize = true;
			this.lblVer.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblVer.Location = new System.Drawing.Point(12, 54);
			this.lblVer.Name = "lblVer";
			this.lblVer.Size = new System.Drawing.Size(132, 13);
			this.lblVer.TabIndex = 34;
			this.lblVer.Text = "Version 1.0.2 (2018-09-03)";
			this.lblVer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lblTitle
			// 
			this.lblTitle.AutoSize = true;
			this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblTitle.Location = new System.Drawing.Point(12, 13);
			this.lblTitle.Margin = new System.Windows.Forms.Padding(3, 0, 3, 5);
			this.lblTitle.Name = "lblTitle";
			this.lblTitle.Size = new System.Drawing.Size(166, 13);
			this.lblTitle.TabIndex = 33;
			this.lblTitle.Text = "AutoTag © James Tattersall 2018";
			this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lnkLblTvDbSharper
			// 
			this.lnkLblTvDbSharper.AutoSize = true;
			this.lnkLblTvDbSharper.Location = new System.Drawing.Point(12, 189);
			this.lnkLblTvDbSharper.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this.lnkLblTvDbSharper.Name = "lnkLblTvDbSharper";
			this.lnkLblTvDbSharper.Size = new System.Drawing.Size(71, 13);
			this.lnkLblTvDbSharper.TabIndex = 39;
			this.lnkLblTvDbSharper.TabStop = true;
			this.lnkLblTvDbSharper.Text = "TvDbSharper";
			this.lnkLblTvDbSharper.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkLblTvDbSharper_LinkClicked);
			// 
			// lnkLblSubtitleFetcher
			// 
			this.lnkLblSubtitleFetcher.AutoSize = true;
			this.lnkLblSubtitleFetcher.Location = new System.Drawing.Point(12, 157);
			this.lnkLblSubtitleFetcher.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.lnkLblSubtitleFetcher.Name = "lnkLblSubtitleFetcher";
			this.lnkLblSubtitleFetcher.Size = new System.Drawing.Size(78, 13);
			this.lnkLblSubtitleFetcher.TabIndex = 40;
			this.lnkLblSubtitleFetcher.TabStop = true;
			this.lnkLblSubtitleFetcher.Text = "SubtitleFetcher";
			this.lnkLblSubtitleFetcher.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkLblSubtitleFetcher_LinkClicked);
			// 
			// lnkLblTaglib
			// 
			this.lnkLblTaglib.AutoSize = true;
			this.lnkLblTaglib.Location = new System.Drawing.Point(12, 173);
			this.lnkLblTaglib.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this.lnkLblTaglib.Name = "lnkLblTaglib";
			this.lnkLblTaglib.Size = new System.Drawing.Size(61, 13);
			this.lnkLblTaglib.TabIndex = 41;
			this.lnkLblTaglib.TabStop = true;
			this.lnkLblTaglib.Text = "taglib-sharp";
			this.lnkLblTaglib.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkLblTaglib_LinkClicked);
			// 
			// lblSubtitleFetcher
			// 
			this.lblSubtitleFetcher.AutoSize = true;
			this.lblSubtitleFetcher.Location = new System.Drawing.Point(90, 157);
			this.lblSubtitleFetcher.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.lblSubtitleFetcher.Name = "lblSubtitleFetcher";
			this.lblSubtitleFetcher.Size = new System.Drawing.Size(216, 13);
			this.lblSubtitleFetcher.TabIndex = 42;
			this.lblSubtitleFetcher.Text = "© Peter Heiberg - Provides file name parsing";
			// 
			// lblTaglib
			// 
			this.lblTaglib.AutoSize = true;
			this.lblTaglib.Location = new System.Drawing.Point(73, 173);
			this.lblTaglib.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.lblTaglib.Name = "lblTaglib";
			this.lblTaglib.Size = new System.Drawing.Size(185, 13);
			this.lblTaglib.TabIndex = 43;
			this.lblTaglib.Text = "© mono Project - Provides file tagging";
			// 
			// lblTvDbSharper
			// 
			this.lblTvDbSharper.AutoSize = true;
			this.lblTvDbSharper.Location = new System.Drawing.Point(83, 189);
			this.lblTvDbSharper.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.lblTvDbSharper.Name = "lblTvDbSharper";
			this.lblTvDbSharper.Size = new System.Drawing.Size(260, 13);
			this.lblTvDbSharper.TabIndex = 44;
			this.lblTvDbSharper.Text = "© Hristo Kolev - Provides thetvdb.com API integration";
			// 
			// lnkLblSource
			// 
			this.lnkLblSource.AutoSize = true;
			this.lnkLblSource.Location = new System.Drawing.Point(12, 67);
			this.lnkLblSource.Margin = new System.Windows.Forms.Padding(3, 0, 3, 5);
			this.lnkLblSource.Name = "lnkLblSource";
			this.lnkLblSource.Size = new System.Drawing.Size(69, 13);
			this.lnkLblSource.TabIndex = 45;
			this.lnkLblSource.TabStop = true;
			this.lnkLblSource.Text = "Source Code";
			this.lnkLblSource.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkLblSource_LinkClicked);
			// 
			// lnkLblWebsite
			// 
			this.lnkLblWebsite.AutoSize = true;
			this.lnkLblWebsite.Location = new System.Drawing.Point(12, 31);
			this.lnkLblWebsite.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
			this.lnkLblWebsite.Name = "lnkLblWebsite";
			this.lnkLblWebsite.Size = new System.Drawing.Size(66, 13);
			this.lnkLblWebsite.TabIndex = 46;
			this.lnkLblWebsite.TabStop = true;
			this.lnkLblWebsite.Text = "jtattersall.net";
			this.lnkLblWebsite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkLblWebsite_LinkClicked);
			// 
			// lblThanks
			// 
			this.lblThanks.AutoSize = true;
			this.lblThanks.Location = new System.Drawing.Point(12, 213);
			this.lblThanks.Name = "lblThanks";
			this.lblThanks.Size = new System.Drawing.Size(315, 13);
			this.lblThanks.TabIndex = 47;
			this.lblThanks.Text = "Thank you to the authors of above projects for their contributions.";
			// 
			// frmAbout
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(392, 235);
			this.Controls.Add(this.lblThanks);
			this.Controls.Add(this.lnkLblWebsite);
			this.Controls.Add(this.lnkLblSource);
			this.Controls.Add(this.lblTvDbSharper);
			this.Controls.Add(this.lblTaglib);
			this.Controls.Add(this.lblSubtitleFetcher);
			this.Controls.Add(this.lnkLblTaglib);
			this.Controls.Add(this.lnkLblSubtitleFetcher);
			this.Controls.Add(this.lnkLblTvDbSharper);
			this.Controls.Add(this.lblSources);
			this.Controls.Add(this.pBoxLogo);
			this.Controls.Add(this.lblChangelog);
			this.Controls.Add(this.lblVer);
			this.Controls.Add(this.lblTitle);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmAbout";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "About";
			((System.ComponentModel.ISupportInitialize)(this.pBoxLogo)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		internal System.Windows.Forms.Label lblSources;
		internal System.Windows.Forms.PictureBox pBoxLogo;
		internal System.Windows.Forms.Label lblChangelog;
		internal System.Windows.Forms.Label lblVer;
		internal System.Windows.Forms.Label lblTitle;
		private System.Windows.Forms.LinkLabel lnkLblTvDbSharper;
		private System.Windows.Forms.LinkLabel lnkLblSubtitleFetcher;
		private System.Windows.Forms.LinkLabel lnkLblTaglib;
		private System.Windows.Forms.Label lblSubtitleFetcher;
		private System.Windows.Forms.Label lblTaglib;
		private System.Windows.Forms.Label lblTvDbSharper;
		private System.Windows.Forms.LinkLabel lnkLblSource;
		private System.Windows.Forms.LinkLabel lnkLblWebsite;
		private System.Windows.Forms.Label lblThanks;
	}
}