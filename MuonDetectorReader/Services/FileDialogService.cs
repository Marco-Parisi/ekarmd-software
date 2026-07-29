using Microsoft.Win32;
using MuonDetectorReader.Services.Interfaces;

namespace MuonDetectorReader.Services
{
    public class FileDialogService : IFileDialogService
    {
        public string ShowOpenFileDialog(string filter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }
    }
}
