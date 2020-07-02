using System;
using System.Windows.Forms;

namespace AutoTag {
    public partial class frmOptions : Form {
        public frmOptions() {
            InitializeComponent();
        }

        private void frmOptions_Load(object sender, EventArgs e) {
            cBoxRenaming.Checked = Properties.Settings.Default.renameFiles;
            gBoxRenaming.Enabled = Properties.Settings.Default.renameFiles;

            tBoxTVPattern.Text = Properties.Settings.Default.renamePatternTV;
            tBoxTVPattern_KeyUp(this, new KeyEventArgs(Keys.A)); // trigger keyup event to show preview

            tBoxMoviePattern.Text = Properties.Settings.Default.renamePatternMovie;
            tBoxMoviePattern_KeyUp(this, new KeyEventArgs(Keys.A)); // trigger keyup event to show preview

            cBoxMetadata.Checked = Properties.Settings.Default.tagFiles;
            gBoxMetadata.Enabled = Properties.Settings.Default.tagFiles;
            cBoxCoverArt.Checked = Properties.Settings.Default.addCoverArt;

            cBoxDefaultMode.SelectedIndex = Properties.Settings.Default.defaultTaggingMode;
        }

        private void SetTVPatternSetting(object sender, EventArgs e) {
            Properties.Settings.Default.renamePatternTV = tBoxTVPattern.Text;
        }

        private void tBoxTVPattern_KeyUp(object sender, KeyEventArgs e) {
            try {
                lblTVPreview.Text = String.Format(tBoxTVPattern.Text.Replace("%1", "{0}").Replace("%2", "{1}").Replace("%3", "{2}").Replace("%4", "{3}"), "Fringe", "2", "08", "August");
            } catch {
                // do nothing, this exception doesn't need to be handled, it can be ignored
            }
        }

        private void SetMoviePatternSetting(object sender, EventArgs e) {
            Properties.Settings.Default.renamePatternMovie = tBoxMoviePattern.Text;
        }

        private void tBoxMoviePattern_KeyUp(object sender, KeyEventArgs e) {
            try {
                lblMoviePreview.Text = String.Format(tBoxMoviePattern.Text.Replace("%1", "{0}").Replace("%2", "{1}"), "Hot Fuzz", "2007");
            } catch {
                // do nothing, this exception doesn't need to be handled, it can be ignored
            }
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

        private void cBoxDefaultMode_SelectedIndexChanged(object sender, EventArgs e) {
            Properties.Settings.Default.defaultTaggingMode = cBoxDefaultMode.SelectedIndex;
        }

        private void btnSave_Click(object sender, EventArgs e) {
            Properties.Settings.Default.Save();
            Close();
        }
    }
}
