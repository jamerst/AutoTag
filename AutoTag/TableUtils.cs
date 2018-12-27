using System;
using System.Windows.Forms;
using System.Drawing;


namespace AutoTag {
	class TableUtils {
		private DataGridView table;

		public TableUtils(DataGridView table) {
			this.table = table;
		}
		public void SetRowError(DataGridViewRow row, string errorMsg) {
			if (row.Cells[1].Value.ToString().Contains("Error")) { // if error already encountered
				SetCellValue(row.Cells[1], row.Cells[1].Value.ToString() + Environment.NewLine + errorMsg);
			}
			else {
				SetCellValue(row.Cells[1], errorMsg);
			}
			SetRowColour(row, "#E57373");
		}

		public String GetRowStatus(DataGridViewRow row) {
			return row.Cells[1].Value.ToString();
		}

		public void SetRowStatus(DataGridViewRow row, string msg) {
			SetCellValue(row.Cells[1], msg);
		}

		public void SetCellValue(DataGridViewCell cell, Object value) {
			table.Invoke(new MethodInvoker(() => cell.Value = value));
		}

		public void SetRowColour(DataGridViewRow row, string hex) {
			table.Invoke(new MethodInvoker(() => row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml(hex)));
		}
	}
}
