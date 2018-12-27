namespace AutoTag {
	partial class frmOptions {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmOptions));
			this.gBoxMetadata = new System.Windows.Forms.GroupBox();
			this.cBoxCoverArt = new System.Windows.Forms.CheckBox();
			this.cBoxMetadata = new System.Windows.Forms.CheckBox();
			this.gBoxRenaming = new System.Windows.Forms.GroupBox();
			this.lblMoviePreview = new System.Windows.Forms.Label();
			this.lblMoviePreviewLabel = new System.Windows.Forms.Label();
			this.lblTVPreview = new System.Windows.Forms.Label();
			this.lblTVPreviewLabel = new System.Windows.Forms.Label();
			this.lblMovieKey = new System.Windows.Forms.Label();
			this.tBoxMoviePattern = new System.Windows.Forms.TextBox();
			this.lblTVKey = new System.Windows.Forms.Label();
			this.tBoxTVPattern = new System.Windows.Forms.TextBox();
			this.lblMoviePattern = new System.Windows.Forms.Label();
			this.lblTVPattern = new System.Windows.Forms.Label();
			this.cBoxRenaming = new System.Windows.Forms.CheckBox();
			this.btnSave = new System.Windows.Forms.Button();
			this.lblDefaultMode = new System.Windows.Forms.Label();
			this.cBoxDefaultMode = new System.Windows.Forms.ComboBox();
			this.gBoxMetadata.SuspendLayout();
			this.gBoxRenaming.SuspendLayout();
			this.SuspendLayout();
			// 
			// gBoxMetadata
			// 
			this.gBoxMetadata.Controls.Add(this.cBoxCoverArt);
			this.gBoxMetadata.Location = new System.Drawing.Point(288, 12);
			this.gBoxMetadata.Name = "gBoxMetadata";
			this.gBoxMetadata.Padding = new System.Windows.Forms.Padding(10, 3, 3, 3);
			this.gBoxMetadata.Size = new System.Drawing.Size(270, 49);
			this.gBoxMetadata.TabIndex = 3;
			this.gBoxMetadata.TabStop = false;
			// 
			// cBoxCoverArt
			// 
			this.cBoxCoverArt.AutoSize = true;
			this.cBoxCoverArt.Location = new System.Drawing.Point(13, 23);
			this.cBoxCoverArt.Name = "cBoxCoverArt";
			this.cBoxCoverArt.Size = new System.Drawing.Size(123, 17);
			this.cBoxCoverArt.TabIndex = 5;
			this.cBoxCoverArt.Text = "Add cover art to files";
			this.cBoxCoverArt.UseVisualStyleBackColor = true;
			this.cBoxCoverArt.CheckedChanged += new System.EventHandler(this.cBoxCoverArt_CheckedChanged);
			// 
			// cBoxMetadata
			// 
			this.cBoxMetadata.AutoSize = true;
			this.cBoxMetadata.Location = new System.Drawing.Point(293, 12);
			this.cBoxMetadata.Name = "cBoxMetadata";
			this.cBoxMetadata.Size = new System.Drawing.Size(149, 17);
			this.cBoxMetadata.TabIndex = 4;
			this.cBoxMetadata.Text = "Write metadata tags to file";
			this.cBoxMetadata.UseVisualStyleBackColor = true;
			this.cBoxMetadata.CheckedChanged += new System.EventHandler(this.cBoxMetadata_CheckedChanged);
			// 
			// gBoxRenaming
			// 
			this.gBoxRenaming.Controls.Add(this.lblMoviePreview);
			this.gBoxRenaming.Controls.Add(this.lblMoviePreviewLabel);
			this.gBoxRenaming.Controls.Add(this.lblTVPreview);
			this.gBoxRenaming.Controls.Add(this.lblTVPreviewLabel);
			this.gBoxRenaming.Controls.Add(this.lblMovieKey);
			this.gBoxRenaming.Controls.Add(this.tBoxMoviePattern);
			this.gBoxRenaming.Controls.Add(this.lblTVKey);
			this.gBoxRenaming.Controls.Add(this.tBoxTVPattern);
			this.gBoxRenaming.Controls.Add(this.lblMoviePattern);
			this.gBoxRenaming.Controls.Add(this.lblTVPattern);
			this.gBoxRenaming.Location = new System.Drawing.Point(12, 12);
			this.gBoxRenaming.Name = "gBoxRenaming";
			this.gBoxRenaming.Padding = new System.Windows.Forms.Padding(10, 3, 3, 3);
			this.gBoxRenaming.Size = new System.Drawing.Size(270, 249);
			this.gBoxRenaming.TabIndex = 5;
			this.gBoxRenaming.TabStop = false;
			// 
			// lblMoviePreview
			// 
			this.lblMoviePreview.AutoSize = true;
			this.lblMoviePreview.Location = new System.Drawing.Point(67, 227);
			this.lblMoviePreview.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
			this.lblMoviePreview.Name = "lblMoviePreview";
			this.lblMoviePreview.Size = new System.Drawing.Size(0, 13);
			this.lblMoviePreview.TabIndex = 9;
			// 
			// lblMoviePreviewLabel
			// 
			this.lblMoviePreviewLabel.AutoSize = true;
			this.lblMoviePreviewLabel.Location = new System.Drawing.Point(13, 227);
			this.lblMoviePreviewLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
			this.lblMoviePreviewLabel.Name = "lblMoviePreviewLabel";
			this.lblMoviePreviewLabel.Size = new System.Drawing.Size(48, 13);
			this.lblMoviePreviewLabel.TabIndex = 8;
			this.lblMoviePreviewLabel.Text = "Preview:";
			// 
			// lblTVPreview
			// 
			this.lblTVPreview.AutoSize = true;
			this.lblTVPreview.Location = new System.Drawing.Point(67, 120);
			this.lblTVPreview.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
			this.lblTVPreview.Name = "lblTVPreview";
			this.lblTVPreview.Size = new System.Drawing.Size(0, 13);
			this.lblTVPreview.TabIndex = 4;
			// 
			// lblTVPreviewLabel
			// 
			this.lblTVPreviewLabel.AutoSize = true;
			this.lblTVPreviewLabel.Location = new System.Drawing.Point(13, 120);
			this.lblTVPreviewLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
			this.lblTVPreviewLabel.Name = "lblTVPreviewLabel";
			this.lblTVPreviewLabel.Size = new System.Drawing.Size(48, 13);
			this.lblTVPreviewLabel.TabIndex = 3;
			this.lblTVPreviewLabel.Text = "Preview:";
			// 
			// lblMovieKey
			// 
			this.lblMovieKey.AutoSize = true;
			this.lblMovieKey.Location = new System.Drawing.Point(13, 191);
			this.lblMovieKey.Name = "lblMovieKey";
			this.lblMovieKey.Size = new System.Drawing.Size(97, 26);
			this.lblMovieKey.TabIndex = 7;
			this.lblMovieKey.Text = "%1 = Title\r\n%2 = Release Year";
			// 
			// tBoxMoviePattern
			// 
			this.tBoxMoviePattern.Location = new System.Drawing.Point(13, 168);
			this.tBoxMoviePattern.Name = "tBoxMoviePattern";
			this.tBoxMoviePattern.Size = new System.Drawing.Size(251, 20);
			this.tBoxMoviePattern.TabIndex = 3;
			this.tBoxMoviePattern.TextChanged += new System.EventHandler(this.SetMoviePatternSetting);
			this.tBoxMoviePattern.KeyUp += new System.Windows.Forms.KeyEventHandler(this.tBoxMoviePattern_KeyUp);
			this.tBoxMoviePattern.Leave += new System.EventHandler(this.SetMoviePatternSetting);
			// 
			// lblTVKey
			// 
			this.lblTVKey.AutoSize = true;
			this.lblTVKey.Location = new System.Drawing.Point(13, 58);
			this.lblTVKey.Name = "lblTVKey";
			this.lblTVKey.Size = new System.Drawing.Size(111, 52);
			this.lblTVKey.TabIndex = 2;
			this.lblTVKey.Text = "%1 = Series Name\r\n%2 = Season Number\r\n%3 = Episode Number\r\n%4 = Episode Name";
			// 
			// tBoxTVPattern
			// 
			this.tBoxTVPattern.Location = new System.Drawing.Point(13, 35);
			this.tBoxTVPattern.Name = "tBoxTVPattern";
			this.tBoxTVPattern.Size = new System.Drawing.Size(251, 20);
			this.tBoxTVPattern.TabIndex = 2;
			this.tBoxTVPattern.TextChanged += new System.EventHandler(this.SetTVPatternSetting);
			this.tBoxTVPattern.KeyUp += new System.Windows.Forms.KeyEventHandler(this.tBoxTVPattern_KeyUp);
			this.tBoxTVPattern.Leave += new System.EventHandler(this.SetTVPatternSetting);
			// 
			// lblMoviePattern
			// 
			this.lblMoviePattern.AutoSize = true;
			this.lblMoviePattern.Location = new System.Drawing.Point(13, 152);
			this.lblMoviePattern.Name = "lblMoviePattern";
			this.lblMoviePattern.Size = new System.Drawing.Size(124, 13);
			this.lblMoviePattern.TabIndex = 5;
			this.lblMoviePattern.Text = "Movie Renaming Pattern";
			// 
			// lblTVPattern
			// 
			this.lblTVPattern.AutoSize = true;
			this.lblTVPattern.Location = new System.Drawing.Point(13, 19);
			this.lblTVPattern.Name = "lblTVPattern";
			this.lblTVPattern.Size = new System.Drawing.Size(109, 13);
			this.lblTVPattern.TabIndex = 0;
			this.lblTVPattern.Text = "TV Renaming Pattern";
			// 
			// cBoxRenaming
			// 
			this.cBoxRenaming.AutoSize = true;
			this.cBoxRenaming.Location = new System.Drawing.Point(17, 11);
			this.cBoxRenaming.Name = "cBoxRenaming";
			this.cBoxRenaming.Size = new System.Drawing.Size(87, 17);
			this.cBoxRenaming.TabIndex = 1;
			this.cBoxRenaming.Text = "Rename files";
			this.cBoxRenaming.UseVisualStyleBackColor = true;
			this.cBoxRenaming.CheckedChanged += new System.EventHandler(this.cBoxRenaming_CheckedChanged);
			// 
			// btnSave
			// 
			this.btnSave.Location = new System.Drawing.Point(483, 239);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(75, 23);
			this.btnSave.TabIndex = 7;
			this.btnSave.Text = "Save";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
			// 
			// lblDefaultMode
			// 
			this.lblDefaultMode.AutoSize = true;
			this.lblDefaultMode.Location = new System.Drawing.Point(290, 70);
			this.lblDefaultMode.Name = "lblDefaultMode";
			this.lblDefaultMode.Size = new System.Drawing.Size(113, 13);
			this.lblDefaultMode.TabIndex = 8;
			this.lblDefaultMode.Text = "Default Tagging Mode";
			// 
			// cBoxDefaultMode
			// 
			this.cBoxDefaultMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cBoxDefaultMode.FormattingEnabled = true;
			this.cBoxDefaultMode.Items.AddRange(new object[] {
            "TV",
            "Movie"});
			this.cBoxDefaultMode.Location = new System.Drawing.Point(409, 67);
			this.cBoxDefaultMode.Name = "cBoxDefaultMode";
			this.cBoxDefaultMode.Size = new System.Drawing.Size(66, 21);
			this.cBoxDefaultMode.TabIndex = 6;
			this.cBoxDefaultMode.SelectedIndexChanged += new System.EventHandler(this.cBoxDefaultMode_SelectedIndexChanged);
			// 
			// frmOptions
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(570, 271);
			this.Controls.Add(this.cBoxDefaultMode);
			this.Controls.Add(this.lblDefaultMode);
			this.Controls.Add(this.btnSave);
			this.Controls.Add(this.cBoxRenaming);
			this.Controls.Add(this.gBoxRenaming);
			this.Controls.Add(this.cBoxMetadata);
			this.Controls.Add(this.gBoxMetadata);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmOptions";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Options";
			this.Load += new System.EventHandler(this.frmOptions_Load);
			this.gBoxMetadata.ResumeLayout(false);
			this.gBoxMetadata.PerformLayout();
			this.gBoxRenaming.ResumeLayout(false);
			this.gBoxRenaming.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox gBoxMetadata;
		private System.Windows.Forms.CheckBox cBoxCoverArt;
		private System.Windows.Forms.CheckBox cBoxMetadata;
		private System.Windows.Forms.GroupBox gBoxRenaming;
		private System.Windows.Forms.Label lblTVKey;
		private System.Windows.Forms.TextBox tBoxTVPattern;
		private System.Windows.Forms.Label lblTVPattern;
		private System.Windows.Forms.CheckBox cBoxRenaming;
		private System.Windows.Forms.Button btnSave;
		private System.Windows.Forms.Label lblTVPreview;
		private System.Windows.Forms.Label lblTVPreviewLabel;
		private System.Windows.Forms.Label lblMoviePreviewLabel;
		private System.Windows.Forms.Label lblMovieKey;
		private System.Windows.Forms.TextBox tBoxMoviePattern;
		private System.Windows.Forms.Label lblMoviePattern;
		private System.Windows.Forms.Label lblMoviePreview;
		private System.Windows.Forms.Label lblDefaultMode;
		private System.Windows.Forms.ComboBox cBoxDefaultMode;
	}
}