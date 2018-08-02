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
			this.lblPreview = new System.Windows.Forms.Label();
			this.lblPreviewLabel = new System.Windows.Forms.Label();
			this.lblKey = new System.Windows.Forms.Label();
			this.tBoxPattern = new System.Windows.Forms.TextBox();
			this.lblPattern = new System.Windows.Forms.Label();
			this.cBoxRenaming = new System.Windows.Forms.CheckBox();
			this.btnSave = new System.Windows.Forms.Button();
			this.gBoxMetadata.SuspendLayout();
			this.gBoxRenaming.SuspendLayout();
			this.SuspendLayout();
			// 
			// gBoxMetadata
			// 
			this.gBoxMetadata.Controls.Add(this.cBoxCoverArt);
			this.gBoxMetadata.Location = new System.Drawing.Point(12, 12);
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
			this.cBoxCoverArt.TabIndex = 0;
			this.cBoxCoverArt.Text = "Add cover art to files";
			this.cBoxCoverArt.UseVisualStyleBackColor = true;
			this.cBoxCoverArt.CheckedChanged += new System.EventHandler(this.cBoxCoverArt_CheckedChanged);
			// 
			// cBoxMetadata
			// 
			this.cBoxMetadata.AutoSize = true;
			this.cBoxMetadata.Location = new System.Drawing.Point(17, 12);
			this.cBoxMetadata.Name = "cBoxMetadata";
			this.cBoxMetadata.Size = new System.Drawing.Size(149, 17);
			this.cBoxMetadata.TabIndex = 4;
			this.cBoxMetadata.Text = "Write metadata tags to file";
			this.cBoxMetadata.UseVisualStyleBackColor = true;
			this.cBoxMetadata.CheckedChanged += new System.EventHandler(this.cBoxMetadata_CheckedChanged);
			// 
			// gBoxRenaming
			// 
			this.gBoxRenaming.Controls.Add(this.lblPreview);
			this.gBoxRenaming.Controls.Add(this.lblPreviewLabel);
			this.gBoxRenaming.Controls.Add(this.lblKey);
			this.gBoxRenaming.Controls.Add(this.tBoxPattern);
			this.gBoxRenaming.Controls.Add(this.lblPattern);
			this.gBoxRenaming.Location = new System.Drawing.Point(12, 68);
			this.gBoxRenaming.Name = "gBoxRenaming";
			this.gBoxRenaming.Padding = new System.Windows.Forms.Padding(10, 3, 3, 3);
			this.gBoxRenaming.Size = new System.Drawing.Size(270, 144);
			this.gBoxRenaming.TabIndex = 5;
			this.gBoxRenaming.TabStop = false;
			// 
			// lblPreview
			// 
			this.lblPreview.AutoSize = true;
			this.lblPreview.Location = new System.Drawing.Point(67, 120);
			this.lblPreview.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
			this.lblPreview.Name = "lblPreview";
			this.lblPreview.Size = new System.Drawing.Size(0, 13);
			this.lblPreview.TabIndex = 4;
			// 
			// lblPreviewLabel
			// 
			this.lblPreviewLabel.AutoSize = true;
			this.lblPreviewLabel.Location = new System.Drawing.Point(13, 120);
			this.lblPreviewLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
			this.lblPreviewLabel.Name = "lblPreviewLabel";
			this.lblPreviewLabel.Size = new System.Drawing.Size(48, 13);
			this.lblPreviewLabel.TabIndex = 3;
			this.lblPreviewLabel.Text = "Preview:";
			// 
			// lblKey
			// 
			this.lblKey.AutoSize = true;
			this.lblKey.Location = new System.Drawing.Point(13, 58);
			this.lblKey.Name = "lblKey";
			this.lblKey.Size = new System.Drawing.Size(111, 52);
			this.lblKey.TabIndex = 2;
			this.lblKey.Text = "{0} = Series Name\r\n{1} = Season Number\r\n{2} = Episode Number\r\n{3} = Episode Name";
			// 
			// tBoxPattern
			// 
			this.tBoxPattern.Location = new System.Drawing.Point(13, 35);
			this.tBoxPattern.Name = "tBoxPattern";
			this.tBoxPattern.Size = new System.Drawing.Size(251, 20);
			this.tBoxPattern.TabIndex = 1;
			this.tBoxPattern.TextChanged += new System.EventHandler(this.tBoxPattern_TextChanged);
			this.tBoxPattern.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tBoxPattern_KeyPress);
			this.tBoxPattern.Leave += new System.EventHandler(this.tBoxPattern_Leave);
			// 
			// lblPattern
			// 
			this.lblPattern.AutoSize = true;
			this.lblPattern.Location = new System.Drawing.Point(13, 19);
			this.lblPattern.Name = "lblPattern";
			this.lblPattern.Size = new System.Drawing.Size(92, 13);
			this.lblPattern.TabIndex = 0;
			this.lblPattern.Text = "Renaming Pattern";
			// 
			// cBoxRenaming
			// 
			this.cBoxRenaming.AutoSize = true;
			this.cBoxRenaming.Location = new System.Drawing.Point(17, 67);
			this.cBoxRenaming.Name = "cBoxRenaming";
			this.cBoxRenaming.Size = new System.Drawing.Size(87, 17);
			this.cBoxRenaming.TabIndex = 6;
			this.cBoxRenaming.Text = "Rename files";
			this.cBoxRenaming.UseVisualStyleBackColor = true;
			this.cBoxRenaming.CheckedChanged += new System.EventHandler(this.cBoxRenaming_CheckedChanged);
			// 
			// btnSave
			// 
			this.btnSave.Location = new System.Drawing.Point(207, 218);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(75, 23);
			this.btnSave.TabIndex = 7;
			this.btnSave.Text = "Save";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
			// 
			// frmOptions
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(294, 253);
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
		private System.Windows.Forms.Label lblKey;
		private System.Windows.Forms.TextBox tBoxPattern;
		private System.Windows.Forms.Label lblPattern;
		private System.Windows.Forms.CheckBox cBoxRenaming;
		private System.Windows.Forms.Button btnSave;
		private System.Windows.Forms.Label lblPreview;
		private System.Windows.Forms.Label lblPreviewLabel;
	}
}