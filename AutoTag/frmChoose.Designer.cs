namespace AutoTag {
	partial class frmChoose {
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this.lblMsg = new System.Windows.Forms.Label();
			this.tblResults = new System.Windows.Forms.DataGridView();
			this.btnContinue = new System.Windows.Forms.Button();
			this.title = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.year = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.tblResults)).BeginInit();
			this.SuspendLayout();
			// 
			// lblMsg
			// 
			this.lblMsg.AutoSize = true;
			this.lblMsg.Location = new System.Drawing.Point(12, 9);
			this.lblMsg.MaximumSize = new System.Drawing.Size(365, 0);
			this.lblMsg.Name = "lblMsg";
			this.lblMsg.Size = new System.Drawing.Size(364, 26);
			this.lblMsg.TabIndex = 0;
			this.lblMsg.Text = "Multiple results were returned, Please choose the correct movie from the list bel" +
    "ow.";
			// 
			// tblResults
			// 
			this.tblResults.AllowUserToAddRows = false;
			this.tblResults.AllowUserToDeleteRows = false;
			this.tblResults.AllowUserToResizeColumns = false;
			this.tblResults.AllowUserToResizeRows = false;
			this.tblResults.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.tblResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.tblResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.title,
            this.year});
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.tblResults.DefaultCellStyle = dataGridViewCellStyle1;
			this.tblResults.Location = new System.Drawing.Point(12, 47);
			this.tblResults.MultiSelect = false;
			this.tblResults.Name = "tblResults";
			this.tblResults.ReadOnly = true;
			this.tblResults.RowHeadersVisible = false;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.tblResults.RowsDefaultCellStyle = dataGridViewCellStyle2;
			this.tblResults.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.tblResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.tblResults.Size = new System.Drawing.Size(360, 173);
			this.tblResults.TabIndex = 1;
			// 
			// btnContinue
			// 
			this.btnContinue.Location = new System.Drawing.Point(12, 226);
			this.btnContinue.Name = "btnContinue";
			this.btnContinue.Size = new System.Drawing.Size(75, 23);
			this.btnContinue.TabIndex = 2;
			this.btnContinue.Text = "Continue";
			this.btnContinue.UseVisualStyleBackColor = true;
			this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
			// 
			// title
			// 
			this.title.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.title.FillWeight = 85F;
			this.title.HeaderText = "Title";
			this.title.Name = "title";
			this.title.ReadOnly = true;
			this.title.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			// 
			// year
			// 
			this.year.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.year.FillWeight = 15F;
			this.year.HeaderText = "Year";
			this.year.Name = "year";
			this.year.ReadOnly = true;
			this.year.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			// 
			// frmChoose
			// 
			this.AcceptButton = this.btnContinue;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(384, 261);
			this.ControlBox = false;
			this.Controls.Add(this.btnContinue);
			this.Controls.Add(this.tblResults);
			this.Controls.Add(this.lblMsg);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "frmChoose";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Input Needed";
			((System.ComponentModel.ISupportInitialize)(this.tblResults)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblMsg;
		private System.Windows.Forms.DataGridView tblResults;
		private System.Windows.Forms.Button btnContinue;
		private System.Windows.Forms.DataGridViewTextBoxColumn title;
		private System.Windows.Forms.DataGridViewTextBoxColumn year;
	}
}