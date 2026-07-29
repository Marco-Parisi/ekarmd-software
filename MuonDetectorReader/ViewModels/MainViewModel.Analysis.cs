using System;
using System.IO;
using System.Linq;

namespace MuonDetectorReader.ViewModels
{
    public partial class MainViewModel
    {
        public void DataProcessingForCLI(string cliPath, string detectorName, int days)
        {
            _silentDataCorrection = true;
            _silentDataCorrDays = days.ToString();

            string mergedPath = _fileParser.MergeFilesCLI(cliPath, detectorName);

            _data = _fileParser.Parse(mergedPath);
            _plotTitle = "File: " + _data.SourceFileName;

            ExecuteDataCorrection();

            DateTime now = DateTime.Now;
            DateTime dateFrom = _data.Dates.Count > 336 ? now - new TimeSpan(days, 0, 0, now.Second) : _data.Dates.Last();
            DateFrom = dateFrom;
            DateTo = _data.Dates.First();

            string imgPath = Path.Combine(cliPath, "img");
            if (!Directory.Exists(imgPath))
                Directory.CreateDirectory(imgPath);

            string[] graphs = { "counts.png", "pressure.png", "temp.png", "rawcount_pressure.png" };

            for (int i = 0; i < graphs.Length; i++)
            {
                if (i == 0) 
                    ExecuteShowGraph("CC");
                else if (i == 1) 
                    ExecuteShowGraph("Press");
                else if (i == 2) 
                    ExecuteShowGraph("Temp");
                else
                {
                    IsDgCgChecked = true;
                    IsDgPChecked = true;
                    ExecuteDoubleGraphOk();
                }

                ExecuteStretchGraph();

                _export.ExportGraphAsPng(CurrentPlotModel, Path.Combine(imgPath, graphs[i]), 900, 500, false);
            }
        }

        private void ExecuteOpenFile(object param)
        {
            string path = _silentDataCorrection ? (string)param : _fileDialog.ShowOpenFileDialog("txt, csv |*.txt;*.csv");

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    bool betaFlag = false;
                    _data = _fileParser.Parse(path);
                    _plotTitle = _localizationService.GetString("Str_FilePrefix") + _data.SourceFileName;

                    _dateToOld = _dateFromOld = DateTo = DateFrom = null;

                    DateMinimum = _data.Dates.Last();
                    DateMaximum = _data.Dates.First();
                    DateFromMaximum = DateMaximum - new TimeSpan(1, 0, 0);
                    DateToMinimum = DateMinimum + new TimeSpan(1, 0, 0);

                    _dateFromOld = DateMinimum;
                    _dateToOld = DateMaximum;
                    DateFrom = DateMinimum;
                    DateTo = DateMaximum;

                    if (_beta.Beta == 0)
                    {
                        ExecuteEstimateBeta();
                        betaFlag = true;
                    }

                    var gaps = _dataCorrection.CheckDataGaps(_data.Dates);
                    if (gaps.Count > 0 && !_silentDataCorrection)
                    {
                        _messageBox.ShowAsync(_localizationService.GetString("Str_MissingDataFound") + "\n\n" + string.Join("", gaps), _localizationService.GetString("Str_MissingData"));
                    }

                    ExecuteDataCorrection();

                    IsBetaPanelEnabled = true;
                    IsOutlierBoxEnabled = true;
                    IsDatePickerEnabled = true;
                    IsExportPanelEnabled = true;
                    _confAreaIsVisible = false; 

                    if (!betaFlag)
                        ExecuteShowGraph("CC");
                }
                catch (Exception ex)
                {
                    _messageBox.Show(_localizationService.GetString("Str_OpenError") + " " + ex.Message, _localizationService.GetString("Str_Error"));
                }
            }
        }

        private void ExecuteDataCorrection()
        {
            if (double.TryParse(ReferencePressureText.Replace(".", ","), out double refPress))
            {
                _dataCorrection.GenerateCorrectedCounts(_data, _beta.Beta, _beta.SigmaBeta, refPress, _kt.KT, _kt.SigmaKT, IsOutlierChecked, OutlierSigmaValue);
            }
        }

        private void ExecuteEstimateBeta()
        {
            if (_data.RawCounts.Count == 0 || !double.TryParse(ReferencePressureText.Replace(".", ","), out double refPress))
                return;

            _beta = _betaCalculation.EstimateBeta(_data.Pressure, _data.RawCounts, refPress);

            ExecuteDataCorrection();

#if KT_ESTIMATION
            if (_data.PressureCorrectedCounts != null && _data.PressureCorrectedCounts.Count > 0)
            {
                _kt = _ktCalculation.EstimateKT(_data.Temperature, _data.PressureCorrectedCounts);
            }
#endif  
            var s = _settings.Load();
            s.Beta = _beta.Beta;
            s.SigmaBeta = _beta.SigmaBeta;
            s.KT = _kt.KT;
            s.SigmaKT = _kt.SigmaKT;
            _settings.Save(s);

            BetaText = _beta.Beta.ToString("0.000000");

            ExecuteDataCorrection();

            _plotTitle = _localizationService.GetString("Str_BetaEstimation");
            CurrentPlotModel = BuildBetaPlotModel();
            _activeGraph = "Beta";
            IsPlotVisible = true;
            IsMessageVisible = false;

            IsGraphPanelEnabled = true;
            IsAvgSliderEnabled = false;
            IsShowHideDataEnabled = false;
            IsTempCorrEnabled = false;
            IsOutlierBoxEnabled = true;
            IsOutlierSliderEnabled = IsOutlierChecked;
            IsStretchButtonVisible = false;
            IsYAxisModeEnabled = false;
        }

        private void OutlierAction()
        {
            ExecuteDataCorrection();
            if (_activeGraph == "Beta")
                ExecuteEstimateBeta();
            else if (!string.IsNullOrEmpty(_activeGraph))
                ExecuteShowGraph(_activeGraph);
        }
    }
}
