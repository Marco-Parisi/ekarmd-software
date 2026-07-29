using Microsoft.Win32;
using MuonDetectorReader.Models;

namespace MuonDetectorReader.ViewModels
{
    public partial class MainViewModel
    {
        private void ExecuteExportGraph()
        {
            if (CurrentPlotModel == null)
                return;

            string path = null;
            SaveFileDialog sfd = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = _activeGraph + ".png" };

            if (sfd.ShowDialog() == true)
                path = sfd.FileName;

            if (!string.IsNullOrEmpty(path))
                _export.ExportGraphAsPng(CurrentPlotModel, path);
        }

        private void ExecuteExportFile()
        {
            if (_data.RawCounts.Count == 0) return;

            SaveFileDialog sfd = new SaveFileDialog { Filter = "Tab-delimited Text|*.txt", FileName = "Export.txt" };
            if (sfd.ShowDialog() == true)
            {
                var corr = new CorrectionResult { PressureCorrectedCounts = _data.PressureCorrectedCounts, FullCorrectedCounts = _data.FullCorrectedCounts, DeltaPressureCorrected = _data.DeltaPressureCorrected, DeltaFullCorrected = _data.DeltaFullCorrected };
                _export.ExportDataFile(_data, _beta, _kt, corr, IsOutlierChecked, OutlierSigmaValue, sfd.FileName);
            }
        }
    }
}
