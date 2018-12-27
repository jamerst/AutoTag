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
		public frmChoose() {
			InitializeComponent();
		}

		public frmChoose(SearchContainer<SearchMovie> movies) {
			InitializeComponent();
			foreach (SearchMovie movie in movies.Results) {
				tblResults.Rows.Add(movie.Title, (movie.ReleaseDate.HasValue) ? movie.ReleaseDate.Value.Year.ToString() : "Unknown");
			}
		}

		public int DisplayDialog() {
			ShowDialog();
			return tblResults.CurrentCell.RowIndex;
		}

		private void btnContinue_Click(object sender, EventArgs e) {
			Close();
		}
	}
}
