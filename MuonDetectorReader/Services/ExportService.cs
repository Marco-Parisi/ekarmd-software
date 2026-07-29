using ControlzEx.Theming;
using MuonDetectorReader.Models;
using MuonDetectorReader.Services.Interfaces;
using OxyPlot;
using OxyPlot.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace MuonDetectorReader.Services
{
    public class ExportService : IExportService
    {
        private readonly IMessageBoxService _messageBoxService;

        public ExportService(IMessageBoxService messageBoxService = null)
        {
            _messageBoxService = messageBoxService ?? new MessageBoxService();
        }

        private void _ExportGraphAsPng(PlotModel model, string path, int width = 1600, int height = 900)
        {
            if (model == null || string.IsNullOrEmpty(path))
                return;

            OxyColor background = model.Background;

            PngExporter.Export(model, path, width, height, background);
        }
        public async void ExportGraphAsPng(PlotModel model, string path, int width = 1600, int height = 900, bool showMsg = true)
        {
            if (showMsg)
            {
                try
                {
                    string date = DateTime.Now.ToString("'_'ddMMyy'_'HHmmss");
                    string folder = System.IO.Path.GetDirectoryName(path);

                    path = path.Insert(path.LastIndexOf('.'), date);

                    _ExportGraphAsPng(model, path, width, height);

                    string msg = string.Format(LocalizationService.Current.GetString("Str_ChartExportSuccess"), path);
                    string title = LocalizationService.Current.GetString("Str_ExportChartTitle");
                    bool openFolder = await _messageBoxService.ShowConfirmAsync(msg, title);

                    if (openFolder)
                        Process.Start(folder);
                }
                catch (Exception ex)
                {
                    _messageBoxService.Show(ex.Message, ex.Source ?? "Error");
                }
            }
            else
                _ExportGraphAsPng(model, path, width, height);
        }

        public async void ExportDataFile(DetectorDataSet data, BetaEstimation beta, KTEstimation kt, CorrectionResult corr, bool outliersEnabled, double outlierSigma, string destinationPath)
        {
            try
            {
                string date = DateTime.Now.ToString("'_'ddMMyy'_'HHmmss");
                string folder = System.IO.Path.GetDirectoryName(destinationPath);

                destinationPath = destinationPath.Insert(destinationPath.LastIndexOf('.'), date);

                using (StreamWriter sw = new StreamWriter(destinationPath))
                {
                    var sb = new StringBuilder();

                    sb.AppendLine(LocalizationService.Current.GetString("Str_FileOriginal") + data.SourceFileName);
                    sb.AppendLine(LocalizationService.Current.GetString("Str_CorrParams"));
                    sb.AppendLine(string.Format("### \tBeta = {1} ± {2}", beta.ReferencePressure, beta.Beta, beta.SigmaBeta));
                    sb.AppendLine(string.Format("### \tkT   = {0} ± {1}", kt.KT, kt.SigmaKT));
                    sb.AppendLine(LocalizationService.Current.GetString("Str_EstimatedFrom"));

                    sb.Append(LocalizationService.Current.GetString("Str_OutlierRemoval"));
                    if (outliersEnabled)
                    {
                        sb.AppendLine(outlierSigma.ToString() + "σ");
                    }
                    else
                    {
                        sb.AppendLine(LocalizationService.Current.GetString("Str_None").ToUpper());
                    }

                    sb.AppendLine("### PCC = Pressure Corrected Counts");
                    sb.AppendLine("### PTCC = Pressure Temperature Corrected Counts");
                    sb.AppendLine(LocalizationService.Current.GetString("Str_TabSeparated"));

                    sb.AppendLine();
                    sb.AppendLine("Data\tTemp\tPress\tCounts\tP.C.C.\tErr P.C.C.\tP.T.C.C.\tErr P.T.C.C.");

                    for (int i = 0; i < data.Dates.Count; i++)
                    {
                        string dateStr = data.Dates[i].ToString().Remove(data.Dates[i].ToString().Length - 3);
                        double temp = data.Temperature[i];
                        double press = data.Pressure[i];
                        double raw = data.RawCounts[i];

                        double pcc = corr.PressureCorrectedCounts != null && corr.PressureCorrectedCounts.Count > i ? corr.PressureCorrectedCounts[i] : 0;
                        double errPcc = corr.DeltaPressureCorrected != null && corr.DeltaPressureCorrected.Count > i ? corr.DeltaPressureCorrected[i] : 0;
                        double ptcc = corr.FullCorrectedCounts != null && corr.FullCorrectedCounts.Count > i ? corr.FullCorrectedCounts[i] : 0;
                        double errPtcc = corr.DeltaFullCorrected != null && corr.DeltaFullCorrected.Count > i ? corr.DeltaFullCorrected[i] : 0;

                        sb.AppendLine($"{dateStr}\t{temp:F2}\t{press:F2}\t{raw}\t{pcc:F2}\t{errPcc:F2}\t{ptcc:F2}\t{errPtcc:F2}");
                    }

                    sw.Write(sb.ToString().Replace(",", "."));
                }

                string msg = string.Format(LocalizationService.Current.GetString("Str_FileExportSuccess"), destinationPath);
                string title = LocalizationService.Current.GetString("Str_ExportFileTitle");
                bool openFolder = await _messageBoxService.ShowConfirmAsync(msg, title);

                if (openFolder)
                    Process.Start(folder);
            }
            catch (Exception ex)
            {
                _messageBoxService.Show(ex.Message, ex.Source ?? "Error");
            }
        }
    }
}
