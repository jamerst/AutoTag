using System;
using System.Windows.Forms;

namespace AutoTag {
	public partial class frmOptions : Form {
		public frmOptions() {
			InitializeComponent();
		}

		private void cBoxMetadata_CheckedChanged(object sender, EventArgs e) {
			gBoxMetadata.Enabled = cBoxMetadata.Checked;
			Properties.Settings.Default.tagFiles = cBoxMetadata.Checked;
		}

		private void cBoxRenaming_CheckedChanged(object sender, EventArgs e) {
			gBoxRenaming.Enabled = cBoxRenaming.Checked;
			Properties.Settings.Default.renameFiles = cBoxRenaming.Checked;
		}

		private void cBoxCoverArt_CheckedChanged(object sender, EventArgs e) {
			Properties.Settings.Default.addCoverArt = cBoxCoverArt.Checked;
		}

		private void tBoxPattern_TextChanged(object sender, EventArgs e) {
			Properties.Settings.Default.renamePattern = tBoxPattern.Text;
		}

		private void tBoxPattern_KeyPress(object sender, KeyPressEventArgs e) {
			try {
				lblPreview.Text = String.Format(tBoxPattern.Text, "Fringe", "2", "08", "August");
			} catch {
				// do nothing, this exception doesn't need to be handled, it can be ignored
			}
			
		}

		private void btnSave_Click(object sender, EventArgs e) {
			Properties.Settings.Default.Save();
			Close();
		}

		private void frmOptions_Load(object sender, EventArgs e) {
			cBoxMetadata.Checked = Properties.Settings.Default.tagFiles;
			gBoxMetadata.Enabled = Properties.Settings.Default.tagFiles;
			cBoxCoverArt.Checked = Properties.Settings.Default.addCoverArt;
			cBoxRenaming.Checked = Properties.Settings.Default.renameFiles;
			gBoxRenaming.Enabled = Properties.Settings.Default.renameFiles;
			tBoxPattern.Text = Properties.Settings.Default.renamePattern;
			tBoxPattern_KeyPress(this, new KeyPressEventArgs(' ')); // trigger keypress event
		}

		private void tBoxPattern_Leave(object sender, EventArgs e) {
			try {
				lblPreview.Text = String.Format(tBoxPattern.Text, "Fringe", "2", "08", "August");
			}
			catch {
				// do nothing, this exception doesn't need to be handled, it can be ignored
			}
		}
	}
}
