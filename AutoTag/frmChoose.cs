using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using TMDbLib.Objects.Search;
using TMDbLib.Objects.General;

namespace AutoTag {
	public partial class frmChoose : Form {
		public int selectedIndex = 0;
		public frmChoose() {
			InitializeComponent();
		}

		public frmChoose(String searchTerm, SearchContainer<SearchMovie> movies) {
			InitializeComponent();
			lblMsg.Text = "Multiple results were returned for \"" + searchTerm + "\". Please choose the correct movie from the list below.";
			foreach (SearchMovie movie in movies.Results) {
				tblResults.Rows.Add(movie.Title, (movie.ReleaseDate.HasValue) ? movie.ReleaseDate.Value.Year.ToString() : "Unknown");
			}
		}

		private void btnContinue_Click(object sender, EventArgs e) {
			selectedIndex = tblResults.CurrentCell.RowIndex;
			Close();
		}

		private void tblResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
			btnContinue.PerformClick();
		}
	}
}
