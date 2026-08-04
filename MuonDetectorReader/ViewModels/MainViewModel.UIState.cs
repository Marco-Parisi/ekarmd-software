using ControlzEx.Theming;
using MuonDetectorReader.Models;
using MuonDetectorReader.Services;
using MuonDetectorReader.Services.Interfaces;
using MuonDetectorReader.Utils;
using MuonDetectorReader.ViewModels.Base;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Media;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace MuonDetectorReader.ViewModels
{
    public partial class MainViewModel
    {
        private void ExecuteShowGraphCommand(string tag)
        {
            _confAreaIsVisible = false;
            ExecuteShowGraph(tag);
        }
        
        private void ExecuteShowGraph(string tag)
        {
            _isYAxisChanged = false;
            _yAxisMajorStep = double.NaN;

            CurrentPlotModel = null;

            if (!_silentDataCorrection)
            {
                DateFrom = DateMinimum;
                DateTo = DateMaximum;
            }

            if (_data.RawCounts.Count == 0)
                return;

            _activeGraph = tag;
            bool isSmoothOn = AvgSliderValue > 0;

            switch (tag)
            {
                case "CG":
                    _plotTitle = _localizationService.GetString("Str_RawCountsTitle");
                    PlotTitleForCLI();
                    CurrentPlotModel = BuildSingleSeriesPlotModel(_data.RawCounts, CGcolor, _localizationService.GetString("Str_RawCountsTitle"), isSmoothOn, (uint)AvgSliderValue);
                    IsTempCorrEnabled = false;
                    break;
                case "CC":
                    _plotTitle = _localizationService.GetString("Str_CorrectedCountsTitle");
                    PlotTitleForCLI();
                    if (IsTempCorrChecked && _data.FullCorrectedCounts.Count > 0)
                    {
                        _plotTitle += " (PT)";
                        CurrentPlotModel = BuildSingleSeriesPlotModel(_data.FullCorrectedCounts, CCcolor, _localizationService.GetString("Str_CorrectedCountsPTTitle"), isSmoothOn, (uint)AvgSliderValue, _data.DeltaFullCorrected);
                    }
                    else
                        CurrentPlotModel = BuildSingleSeriesPlotModel(_data.PressureCorrectedCounts, CCcolor, _localizationService.GetString("Str_CorrectedCountsTitle"), isSmoothOn, (uint)AvgSliderValue, _data.DeltaPressureCorrected);
                    IsTempCorrEnabled = true;
                    break;
                case "Press":
                    _plotTitle = _localizationService.GetString("Str_PressureTitle");
                    PlotTitleForCLI();
                    CurrentPlotModel = BuildSingleSeriesPlotModel(_data.Pressure, Pcolor, _localizationService.GetString("Str_PressureUnitTitle"), false, (uint)AvgSliderValue);
                    IsTempCorrEnabled = false;
                    break;
                case "Temp":
                    _plotTitle = _localizationService.GetString("Str_TemperatureTitle");
                    PlotTitleForCLI();
                    CurrentPlotModel = BuildSingleSeriesPlotModel(_data.Temperature, Tcolor, _localizationService.GetString("Str_TemperatureUnitTitle"), false, (uint)AvgSliderValue);
                    IsTempCorrEnabled = false;
                    break;
            }

            IsGraphPanelEnabled = true;
            IsYAxisScaleEnabled = IsAvgSliderEnabled = tag != "Press" && tag != "Temp";
            IsShowHideDataEnabled = isSmoothOn && tag != "Press" && tag != "Temp";
            IsOutlierBoxEnabled = true; // (tag != "Press" && tag != "Temp");
            IsOutlierSliderEnabled = IsOutlierBoxEnabled && IsOutlierChecked;
            IsYAxisModeEnabled = true;
            IsStretchButtonVisible = (tag == "CG" || tag == "CC" || tag == "Press" || tag == "Temp");
            IsResetButtonVisible = false;
            IsDatePickerEnabled = true;

            if (IsShowHideDataChecked) 
                ExecuteToggleShowHideData();

            IsPlotVisible = true;
            IsMessageVisible = false;
        }

        private void ExecuteDoubleGraphOk()
        {
            IsDoubleGraphFlyoutOpen = false;

            CurrentPlotModel = null;

            if (!_silentDataCorrection)
            {
                DateFrom = DateMinimum;
                DateTo = DateMaximum;
            }

            string tag1 = "", tag2 = "";
            List<double> data1 = null, data2 = null;
            Color c1 = Colors.Black, c2 = Colors.Black;
            string t1 = "", t2 = "";

            if (IsDgCgChecked) { tag1 = "CG"; data1 = _data.RawCounts; c1 = CGcolor; t1 = _localizationService.GetString("Str_RawCountsTitle"); }
            else if (IsDgCcpChecked) { tag1 = "CC"; data1 = _data.PressureCorrectedCounts; c1 = CCcolor; t1 = _localizationService.GetString("Str_CorrectedCountsP"); }
            else if (IsDgCcptChecked) { tag1 = "CCT"; data1 = _data.FullCorrectedCounts; c1 = CCcolor; t1 = _localizationService.GetString("Str_CorrectedCountsPT"); }

            if (IsDgPChecked) { tag2 = "P"; data2 = _data.Pressure; c2 = Pcolor; t2 = _localizationService.GetString("Str_PressureUnitTitle"); }
            else if (IsDgTChecked) { tag2 = "T"; data2 = _data.Temperature; c2 = Tcolor; t2 = _localizationService.GetString("Str_TemperatureUnitTitle"); }

            if (data1 != null && data2 != null)
            {
                string title1 = tag1 == "CG" ? _localizationService.GetString("Str_RawCountsTitle") : _localizationService.GetString("Str_CorrectedCountsTitle");
                string title2 = tag2 == "P" ? _localizationService.GetString("Str_PressureTitle") : _localizationService.GetString("Str_TemperatureTitle");
                _plotTitle = $"{title1} vs {title2}";

                PlotTitleForCLI();

                CurrentPlotModel = BuildDualSeriesPlotModel(data1, data2, t1, t2, c1, c2);
                _activeGraph = tag1 + tag2;
                IsAvgSliderEnabled = false;
                IsShowHideDataEnabled = false;
                IsTempCorrEnabled = false;
                IsOutlierBoxEnabled = false;
                IsOutlierSliderEnabled = false;
                IsStretchButtonVisible = true;
                IsPlotVisible = true;
                IsMessageVisible = false;
                IsYAxisModeEnabled = true;
                IsDatePickerEnabled = true;
                IsYAxisScaleEnabled = false;
            }
        }

        private void PlotTitleForCLI() 
        {
            if (_silentDataCorrection)
                _plotTitle += $" {{Δt = {_silentDataCorrDays} {_localizationService.GetString("Str_Days")}}}";
        }

        private void ToggleYAxisTypeAction()
        {
            if (_activeGraph == "CG" || _activeGraph == "CC")
            {
                _confAreaIsVisible = false;
                ExecuteShowGraph(_activeGraph);
            }
        }
        
        private void PreserveZoomAndExecute(Action action)
        {
            var prevBounds = new Dictionary<AxisPosition, (double Min, double Max)>();
            if (CurrentPlotModel != null)
            {
                foreach (var axis in CurrentPlotModel.Axes)
                {
                    prevBounds[axis.Position] = (axis.ActualMinimum, axis.ActualMaximum);
                }
            }

            bool wasResetVisible = IsResetButtonVisible;
            //bool wasStretchVisible = IsStretchButtonVisible;

            bool tempFlag = _isYAxisChanged;
            double tempYAxMS = _yAxisMajorStep;

            bool confArea = _confAreaIsVisible;
            
            action();

            _isYAxisChanged = tempFlag;
            _yAxisMajorStep = tempYAxMS;

            if (CurrentPlotModel != null && prevBounds.Count > 0)
            {
                if (_confidenceArea != null)
                {
                    if (confArea)
                        CurrentPlotModel.Annotations.Add(_confidenceArea);
                    _confAreaIsVisible = confArea;
                }

                foreach (var axis in CurrentPlotModel.Axes)
                {
                    if (axis is DateTimeAxis || _isYAxisChanged)
                    {
                        if (prevBounds.TryGetValue(axis.Position, out var bounds))
                        {
                            if (!double.IsNaN(bounds.Min) && !double.IsNaN(bounds.Max))
                            {
                                axis.Minimum = bounds.Min;
                                axis.Maximum = bounds.Max;
                            }
                        }
                    }
                }
                CurrentPlotModel.InvalidatePlot(false);
                IsResetButtonVisible = wasResetVisible;
                IsStretchButtonVisible = true;//wasStretchVisible;
            }
        }

        private void ExecuteToggleTempCorr()
        {
            if (_activeGraph == "CC" || _activeGraph == "SIGMA")
            {
                PreserveZoomAndExecute(() => ExecuteShowGraph(_activeGraph));
            }
        }

        private void ExecuteToggleShowHideData()
        {
            if (CurrentPlotModel == null) return;
            var series = CurrentPlotModel.Series.FirstOrDefault() as LineSeries;
            if (series != null)
            {
                series.IsVisible = !IsShowHideDataChecked;

                if (_confidenceArea != null)
                {
                    if (IsShowHideDataChecked)
                        CurrentPlotModel.Annotations.Remove(_confidenceArea);
                    else if (!CurrentPlotModel.Annotations.Contains(_confidenceArea) && _confAreaIsVisible)
                        CurrentPlotModel.Annotations.Add(_confidenceArea);
                }
                CurrentPlotModel.InvalidatePlot(false);
            }
        }

        private void ExecuteResetGraph()
        {
            if (CurrentPlotModel != null && _initialAxisBounds != null)
            {
                foreach (var axis in CurrentPlotModel.Axes)
                {
                    if (_initialAxisBounds.TryGetValue(axis, out var bounds))
                    {
                        axis.Minimum = bounds.Min;
                        axis.Maximum = bounds.Max;
                        axis.Reset();
                    }
                }

                Axis xAxis = CurrentPlotModel.Axes.Last();

                xAxis.MajorStep = _yAxisMajorStep;
                if (_activeGraph != "Beta")
                    xAxis.StringFormat = xAxis.MajorStep < 0.5 ? "yyyy/MM/dd - HH:mm" : "yyyy/MM/dd";

                _dateFrom = DateMinimum;
                _dateTo = DateMaximum;
                OnPropertyChanged(nameof(DateFrom));
                OnPropertyChanged(nameof(DateTo));

                CurrentPlotModel.InvalidatePlot(false);
                IsResetButtonVisible = false;
                IsStretchButtonVisible = _activeGraph != "Beta";
            }
        }

        private void ExecuteStretchGraph()
        {
            if (CurrentPlotModel == null) 
                return;

            var xAxis = CurrentPlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            var yAxis = CurrentPlotModel.Axes.Where(a => a.Position != AxisPosition.Bottom);
            
            var seriesList = CurrentPlotModel.Series;
            int i = 0;

            foreach (LineSeries series in seriesList)
            {
                if (xAxis != null && yAxis.ElementAt(i) != null && series != null)
                {
                    double xMin = _silentDataCorrection ? xAxis.Minimum : xAxis.ActualMinimum;
                    double xMax = _silentDataCorrection ? xAxis.Maximum: xAxis.ActualMaximum;

                    var visiblePoints = series.Points.Where(p => p.X >= xMin && p.X <= xMax).ToList();
                    if (visiblePoints.Any())
                    {
                        double yMin = visiblePoints.Min(p => p.Y);
                        double yMax = visiblePoints.Max(p => p.Y);
                        double yDiff = Math.Abs(yMax - yMin);

                        double yFact = _silentDataCorrection ? 0.2 : 0.05;

                        //double yMed = visiblePoints.Average(d => d.Y);
                        //int digit = Math.Truncate(yMed).ToString("0.").Length;

                        //yAxis.Minimum = yMin - (yMed / Math.Pow(10, digit) * 2);
                        //yAxis.Maximum = yMax + (yMed / Math.Pow(10, digit) * 2);

                        yAxis.ElementAt(i).Maximum = yMax + yFact * yDiff;
                        yAxis.ElementAt(i).Minimum = yMin - yFact * yDiff;

                        yAxis.ElementAt(i).Reset();
                        CurrentPlotModel.InvalidatePlot(false);
                        IsResetButtonVisible = true;
                        IsStretchButtonVisible = false;
                    }
                    i++;
                }
                if (!_doubleGraphActive)
                    break;
            }
        }

        private void ExecuteToggleLanguage()
        {
            string newLang = _localizationService.CurrentLanguage == "it" ? "en" : "it";
            _localizationService.ChangeLanguage(newLang);
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(ToggleSidebarTooltip));
            OnPropertyChanged(nameof(BetaText));
            OnPropertyChanged(nameof(SmoothValueText));
            OnPropertyChanged(nameof(ChartModeText));

            if (_activeGraph == "Beta")
            {
                _plotTitle = _localizationService.GetString("Str_BetaEstimation");
                PreserveZoomAndExecute(() => {
                    CurrentPlotModel = BuildBetaPlotModel();
                });
            }
            else if (_activeGraph == "CG" || _activeGraph == "CC" || _activeGraph == "Press" || _activeGraph == "Temp")
            {
                PreserveZoomAndExecute(() => ExecuteShowGraph(_activeGraph));
            }
            else if (!string.IsNullOrEmpty(_activeGraph) && _activeGraph.Length > 2)
            {
                PreserveZoomAndExecute(() => ExecuteDoubleGraphOk());
            }
        }

        private void ExecuteShowHelp()
        {
            IsPlotVisible = !IsPlotVisible;
            IsMessageVisible = !IsPlotVisible;
        }
    }
}
