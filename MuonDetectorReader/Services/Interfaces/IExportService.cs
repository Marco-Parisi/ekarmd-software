using OxyPlot;

namespace MuonDetectorReader.Services.Interfaces
{
    public interface IExportService
    {
        void ExportGraphAsPng(PlotModel model, string path, int width = 1600, int height = 900, bool showMsg = true);
        void ExportDataFile(Models.DetectorDataSet data, Models.BetaEstimation beta, Models.KTEstimation kt, Models.CorrectionResult corr, bool outliersEnabled, double outlierSigma, string sourceFileName);
    }
}
