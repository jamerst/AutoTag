using System.Windows.Forms;
using System.Diagnostics;

namespace AutoTag {
	public partial class frmAbout : Form {
		public frmAbout() {
			InitializeComponent();
		}

		private void lnkLblSubtitleFetcher_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			lnkLblSubtitleFetcher.LinkVisited = true;
			Process.Start("https://github.com/HristoKolev/TvDbSharper");
		}

		private void lnkLblTaglib_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			lnkLblTaglib.LinkVisited = true;
			Process.Start("https://github.com/mono/taglib-sharp");
		}

		private void lnkLblTvDbSharper_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			lnkLblTvDbSharper.LinkVisited = true;
			Process.Start("https://github.com/HristoKolev/TvDbSharper");
		}

		private void lnkLblWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			lnkLblWebsite.LinkVisited = true;
			Process.Start("http://jtattersall.net/");
		}

		private void lnkLblSource_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			lnkLblSource.LinkVisited = true;
			Process.Start("https://github.com/jamerst/AutoTag");
		}
	}
}
