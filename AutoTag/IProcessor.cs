using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoTag {
    public interface IProcessor {
        Task<FileMetadata> process(TableUtils utils, DataGridViewRow row, frmMain mainForm);
    }
}