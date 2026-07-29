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
        private void InitializePlotController()
        {
            PlotController = new PlotController();
            PlotController.BindMouseEnter(PlotCommands.HoverSnapTrack);
            PlotController.UnbindMouseDown(OxyMouseButton.Right);
            PlotController.BindMouseDown(OxyMouseButton.Right, PlotCommands.ZoomRectangle);

            BindMousePan();
            BindMouseWheelZoom();
        }

        private void BindMousePan() 
        {
            PlotController.UnbindMouseDown(OxyMouseButton.Left);

            var panCommand = new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
            {
                var model = view.ActualModel;
                if (model == null) 
                    return;

                controller.AddMouseManipulator(view, new AxisManipulator(view, args.Position, model), args);
            });

            PlotController.BindMouseDown(OxyMouseButton.Left, panCommand);
        }

        private void BindMouseWheelZoom()
        {
            PlotController.UnbindMouseWheel();

            var zoomCommand = new DelegatePlotCommand<OxyMouseWheelEventArgs>((view, controller, args) =>
            {
                if (!_doubleGraphActive)
                {
                    PlotCommands.ZoomWheel.Execute(view, controller, args);
                    return;
                }

                var model = view.ActualModel;
                if (model == null) 
                    return;
                
                double scaleFactor = 1 + (args.Delta * 0.001);
                foreach (var axis in model.Axes)
                {
                    if (!axis.IsZoomEnabled)
                        continue;
                    
                    if (axis.IsVertical())
                    {
                        double coordinate = axis.InverseTransform(args.Position.Y);
                        axis.ZoomAt(scaleFactor, coordinate);
                    }
                    else if (axis.IsHorizontal())
                    {
                        double coordinate = axis.InverseTransform(args.Position.X);
                        axis.ZoomAt(scaleFactor, coordinate);
                    }
                }

                view.InvalidatePlot(false);
                args.Handled = true;
            });

            PlotController.BindMouseWheel(OxyModifierKeys.None, zoomCommand);
        }

        private PlotModel BuildSingleSeriesPlotModel(List<double> data, Color color, string parName, bool smooth, uint window, List<double> errData = null)
        {
            var oxC = OxyColor.FromArgb(color.A, color.R, color.G, color.B);
            _confareaColor = oxC;
            _confidenceArea = null;
            _smoothAnnotation = null;
            _doubleGraphActive = false;

            _xAxisStringFormat = _confAreaIsVisible ? "yyyy/MM/dd - HH:mm" : "yyyy/MM/dd";

            bool isCounts = parName.Contains("Conteggi") || parName.Contains("Counts") || parName.Contains("Cont.");

            double dateMax = DateTimeAxis.ToDouble(_silentDataCorrection ? DateTo : _data.Dates.Max());
            double dateMin = DateTimeAxis.ToDouble(_silentDataCorrection ? DateFrom : _data.Dates.Min());
            double dateDiff = Math.Abs(dateMax - dateMin);

            _dateFromOld = _data.Dates.Min();
            _dateToOld = _data.Dates.Max();

            double dMean = data.Average();
            double dMax = data.Max();
            double dMin = data.Min();
            double dDiff;

            string legDec = isCounts && _isYAxisAbsolute ? "0." : "0.00";

            string UnitLabel = isCounts ? "" : (" (" + parName.Split('(').Last());
            string legUnit = isCounts ? (_isYAxisSigma ? "σ" : (_isYAxisPercentage ? "%" : "")) : UnitLabel;
            string LegendString;

            if (isCounts)
            {
                if (_isYAxisSigma)
                {
                    var sigmas = errData;
                    if (sigmas == null)
                    {
                        sigmas = data.Select(c => Math.Sqrt(Math.Max(c, 1.0))).ToList();
                    }
                    data = data.Zip(sigmas, (c, e) => (c - dMean) / e).ToList();
                    errData = Enumerable.Repeat(1.0, data.Count).ToList();
                }
                else if (_isYAxisPercentage)
                {
                    data = data.Select(d => (d - dMean) * 100 / dMean).ToList();
                    if (errData != null)
                        errData = errData.Select(d => d * 100 / dMean).ToList();
                }
                else
                    errData = data.Select(d => Math.Sqrt(d)).ToList();

                dMax = data.Max();
                dMin = data.Min();
            }

            dDiff = Math.Abs(dMax - dMin);

            LegendString = "Max: " + dMax.ToString(legDec) + legUnit;
            LegendString += "\nMin: " + dMin.ToString(legDec) + legUnit;
            LegendString += (_isYAxisAbsolute || !isCounts) ? ("\nAvg: " + dMean.ToString(legDec) + legUnit) : "";

            var model = new PlotModel
            {
                PlotType = PlotType.XY,
                PlotMargins = new OxyThickness(double.NaN, 0, double.NaN, double.NaN),

                Title = _plotTitle,
                Subtitle = _silentDataCorrection ? "" : $"{_localizationService.GetString("Str_FilePrefix")}{_data.SourceFileName} {_localizationService.GetString("Str_DataCount")}{_data.Dates.Count}",
                TitleFontSize = 14,

                LegendTitleFontSize = 12,
                LegendTitle = _silentDataCorrection? "" : LegendString,
                LegendMaxHeight = 23 + (20 * LegendString.Split('\n').Length),
                LegendPosition = LegendPosition.TopRight,

            };

            var series = new LineSeries
            {
                CanTrackerInterpolatePoints = false,
                Color = oxC,
                LineStyle = _isSeriesDottedChecked ? LineStyle.None : LineStyle.Solid,
                MarkerType = _isSeriesDottedChecked ? MarkerType.Circle : MarkerType.None,
                MarkerSize = 3,
                MarkerFill = _isSeriesDottedChecked ? oxC : OxyColors.Automatic,
                Decimator = Decimator.Decimate,
            };

            if (_silentDataCorrection && isCounts)
            {
                series.LineStyle = LineStyle.None;
                series.MarkerType = MarkerType.Circle;
                series.MarkerSize = 3;
                series.MarkerFill = oxC;
            }

            var upper = new List<DataPoint>();
            var lower = new List<DataPoint>();

            for (int i = 0; i < _data.Dates.Count; i++)
            {
                double x = DateTimeAxis.ToDouble(_data.Dates[i]);
                series.Points.Add(new DataPoint(x, data[i]));

                if (errData != null)
                {
                    upper.Add(new DataPoint(x, data[i] + errData[i]));
                    lower.Add(new DataPoint(x, data[i] - errData[i]));
                }
            }

            if (errData != null)
            {
                var polyPoints = new List<DataPoint>(lower);
                polyPoints.AddRange(upper.AsEnumerable().Reverse());

                _confidenceArea = new PolygonAnnotation
                {
                    Fill = OxyColor.FromArgb(80, oxC.R, oxC.G, oxC.B),
                    StrokeThickness = 0,
                    Layer = AnnotationLayer.BelowSeries
                };
                _confidenceArea.Points.AddRange(polyPoints);
                if ((_data.Dates.Max() - _data.Dates.Min()).TotalDays <= _confAreaDaysMin)
                {
                    model.Annotations.Add(_confidenceArea); 
                    _confAreaIsVisible = true;
                }
            }

            model.Series.Add(series);

            var avgLine = new LineSeries
            {
                Color = OxyColors.Black,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 1,
                TrackerFormatString = _localizationService.GetString("Str_MeanValue") + dMean.ToString("0.00") + UnitLabel,
            };
            double avgY = (isCounts && !_isYAxisAbsolute) ? 0 : dMean;
            avgLine.Points.Add(new DataPoint(DateTimeAxis.ToDouble(_data.Dates.Min()), avgY));
            avgLine.Points.Add(new DataPoint(DateTimeAxis.ToDouble(_data.Dates.Max()), avgY));
            model.Series.Add(avgLine);

            var yAxisTitle = parName;
            if (isCounts)
            {
                if (_isYAxisSigma) yAxisTitle = _localizationService.GetString("Str_StandardDeviations");
                else if (_isYAxisPercentage) yAxisTitle = _localizationService.GetString("Str_PercentFromAvg");
                else yAxisTitle = _localizationService.GetString("Str_AbsoluteCounts");
            }

            var yAxis = new LinearAxis
            {
                Title = yAxisTitle,
                AxisTitleDistance = 20,
                AbsoluteMaximum = dMax + dDiff,
                AbsoluteMinimum = dMin - dDiff,
                Maximum = dMax + 0.2 * dDiff,
                Minimum = dMin - 0.2 * dDiff,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                MinimumMajorStep = 0.1,
                IntervalLength = 30
            };
            yAxis.AxisChanged += (s, e) => 
            { 
                IsResetButtonVisible = true; 
                //IsStretchButtonVisible = false;
                _isYAxisChanged = true;
            };

            series.TrackerFormatString = parName + " : " + (_activeGraph == "CG" && _isYAxisAbsolute ? "{Y:0.}" : "{Y:0.00}");
            if (isCounts && !_isYAxisAbsolute)
            {
                string label = _isYAxisSigma ? "σ" : (_isYAxisPercentage ? "%" : "");
                series.TrackerFormatString += label;
                yAxis.LabelFormatter = x => x.ToString("0.0") + label;
            }

            series.TrackerFormatString += Environment.NewLine + "Data: {2:yyyy/MM/dd} - {2:HH:mm}";

            model.Axes.Add(yAxis);

            var xAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = _xAxisStringFormat,
                IntervalType = DateTimeIntervalType.Days,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                Angle = -35,
                MinimumMajorStep = 1 / 24.0,
                Maximum = dateMax + 0.005 * dateDiff,
                Minimum = dateMin - 0.005 * dateDiff,
                AbsoluteMaximum = dateMax + 0.1 * dateDiff,
                AbsoluteMinimum = dateMin - 0.1 * dateDiff,
            };

            xAxis.AxisChanged += (s, e) =>
            {
                IsResetButtonVisible = true;
                IsStretchButtonVisible = true;
                _xAxisManipulation = true;

                _yAxisMajorStep = double.IsNaN(_yAxisMajorStep) ? xAxis.ActualMajorStep : _yAxisMajorStep;

                if (xAxis.ActualMajorStep > 1)
                    xAxis.MajorStep = (_yAxisMajorStep * (xAxis.ActualMaximum - xAxis.ActualMinimum)) / (series.MaxX - series.MinX);
                else
                    xAxis.MajorStep = Math.Ceiling(1.5*(xAxis.ActualMaximum - xAxis.ActualMinimum)) * xAxis.MinimumMajorStep;

                xAxis.StringFormat = xAxis.MajorStep < 0.5 ? "yyyy/MM/dd - HH:mm" : "yyyy/MM/dd";
                _xAxisStringFormat = xAxis.StringFormat;

                DateTime Dt = DateTimeAxis.ToDateTime(xAxis.ActualMaximum);
                DateTo = new DateTime(Dt.Year, Dt.Month, Dt.Day, Dt.Hour, 0, 0);

                Dt = DateTimeAxis.ToDateTime(xAxis.ActualMinimum);
                DateFrom = new DateTime(Dt.Year, Dt.Month, Dt.Day, Dt.Hour, 0, 0);

                if (xAxis.ActualMaximum - xAxis.ActualMinimum <= _confAreaDaysMin)
                {
                    if (_confidenceArea != null && !model.Annotations.Contains(_confidenceArea) && !IsShowHideDataChecked)
                    {
                        model.Annotations.Add(_confidenceArea);
                        _confAreaIsVisible = true;
                    }
                }
                else
                {
                    if (_confidenceArea != null && model.Annotations.Contains(_confidenceArea))
                    {
                        model.Annotations.Remove(_confidenceArea);
                        _confAreaIsVisible = false;
                    }
                }

                model.InvalidatePlot(false);
                _xAxisManipulation = false;
            };

            model.Axes.Add(xAxis);

            if (smooth)
            {
                _smoothAnnotation = CreateSmoothedAnnotation(series.Points.ToList(), window, oxC);
                model.Annotations.Add(_smoothAnnotation);
            }

            ApplyThemeToPlotModel(model);
            return model;
        }

        private PlotModel BuildDualSeriesPlotModel(List<double> data1, List<double> data2, string title1, string title2, Color c1, Color c2)
        {
            var oxC1 = OxyColor.FromArgb(c1.A, c1.R, c1.G, c1.B);
            var oxC2 = OxyColor.FromArgb(c2.A, c2.R, c2.G, c2.B);

            double m1 = data1.Average();
            double d1Max = data1.Max();
            double d1Min = data1.Min();
            double d1Diff = Math.Abs(d1Max - d1Min);
            double d2Max = data2.Max();
            double d2Min = data2.Min();
            double d2Diff = Math.Abs(d2Max - d2Min);

            double dateMax = DateTimeAxis.ToDouble(_silentDataCorrection ? DateTo : _data.Dates.Max());
            double dateMin = DateTimeAxis.ToDouble(_silentDataCorrection ? DateFrom : _data.Dates.Min());
            double dateDiff = Math.Abs(dateMax - dateMin);

            _dateFromOld = _data.Dates.Min();
            _dateToOld = _data.Dates.Max();

            _doubleGraphActive = true;

            _xAxisStringFormat = _confAreaIsVisible ? "yyyy/MM/dd - HH:mm" : "yyyy/MM/dd";

            var model = new PlotModel
            {
                PlotType = PlotType.XY,
                Title = _plotTitle,
                Subtitle = _silentDataCorrection ? "" : $"{_localizationService.GetString("Str_FilePrefix")}{_data.SourceFileName}{_localizationService.GetString("Str_DataCount")}{_data.Dates.Count}",
                TitleFontSize = 14,
            };

            var s1 = new LineSeries 
            { 
                Color = oxC1, 
                Title = title1,
                LineStyle = _isSeriesDottedChecked ? LineStyle.None : LineStyle.Solid,
                MarkerType = _isSeriesDottedChecked ? MarkerType.Circle : MarkerType.None,
                MarkerSize = 3,
                MarkerFill = _isSeriesDottedChecked ? oxC1 : OxyColors.Automatic,
                Decimator = Decimator.Decimate
            };
            var s2 = new LineSeries 
            { 
                Color = oxC2, 
                Title = title2, 
                YAxisKey = "Y2",
                LineStyle = _isSeriesDottedChecked ? LineStyle.None : LineStyle.Solid,
                MarkerType = _isSeriesDottedChecked ? MarkerType.Circle : MarkerType.None,
                MarkerSize = 3,
                MarkerFill = _isSeriesDottedChecked ? oxC2 : OxyColors.Automatic,
                Decimator = Decimator.Decimate
            };

            for (int i = 0; i < _data.Dates.Count; i++)
            {
                double x = DateTimeAxis.ToDouble(_data.Dates[i]);
                s1.Points.Add(new DataPoint(x, data1[i]));
                s2.Points.Add(new DataPoint(x, data2[i]));
            }

            model.Series.Add(s1);
            model.Series.Add(s2);

            var y1 = new LinearAxis 
            { 
                Position = AxisPosition.Left, 
                Title = _localizationService.GetString("Str_AbsoluteCounts"), 
                TitleColor = oxC1, 
                TextColor = oxC1,
                MinimumMajorStep = 0.1,
                IntervalLength = 30,
                AxisTitleDistance = 20,
                AbsoluteMaximum = d1Max + d1Diff,
                AbsoluteMinimum = d1Min - d1Diff,
                Maximum = d1Max + 0.3 * d1Diff,
                Minimum = d1Min - 0.3 * d1Diff,
            };

            var y2 = new LinearAxis 
            {
                Position = AxisPosition.Right,
                Key = "Y2", 
                Title = title2,
                TitleColor = oxC2, 
                TextColor = oxC2,
                MinimumMajorStep = 0.1,
                IntervalLength = 30,
                AxisTitleDistance = 20,
                AbsoluteMaximum = d2Max + d2Diff,
                AbsoluteMinimum = d2Min - d2Diff,
                Maximum = d2Max + 0.3 * d2Diff,
                Minimum = d2Min - 0.3 * d2Diff,
            };

            y1.AxisChanged += (s, e) => { IsResetButtonVisible = true; };
            y2.AxisChanged += (s, e) => { IsResetButtonVisible = true; };

            

            model.Axes.Add(y1);
            model.Axes.Add(y2);

            var xAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = _xAxisStringFormat,
                IntervalType = DateTimeIntervalType.Days,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                Angle = -35,
                MinimumMajorStep = 1 / 24.0,
                Maximum = dateMax,
                Minimum = dateMin,
                AbsoluteMaximum = dateMax + 0.1 * dateDiff,
                AbsoluteMinimum = dateMin - 0.1 * dateDiff,
            };

            double majorStep = double.NaN;
            xAxis.AxisChanged += (s, e) => 
            {
                IsResetButtonVisible = true;
                IsStretchButtonVisible = true;
                _xAxisManipulation = true;

                majorStep = double.IsNaN(majorStep) ? xAxis.ActualMajorStep : majorStep;

                if (xAxis.ActualMajorStep > 1)
                    xAxis.MajorStep = (majorStep * (xAxis.ActualMaximum - xAxis.ActualMinimum)) / (s1.MaxX - s1.MinX);
                else
                    xAxis.MajorStep = Math.Ceiling(1.5*(xAxis.ActualMaximum - xAxis.ActualMinimum)) * xAxis.MinimumMajorStep;

                xAxis.StringFormat = xAxis.MajorStep < 0.5 ? "yyyy/MM/dd - HH:mm" : "yyyy/MM/dd";
                _xAxisStringFormat = xAxis.StringFormat;

                DateTime Dt = DateTimeAxis.ToDateTime(xAxis.ActualMaximum);
                DateTo = new DateTime(Dt.Year, Dt.Month, Dt.Day, Dt.Hour, 0, 0);

                Dt = DateTimeAxis.ToDateTime(xAxis.ActualMinimum);
                DateFrom = new DateTime(Dt.Year, Dt.Month, Dt.Day, Dt.Hour, 0, 0);

                model.InvalidatePlot(false);
                _xAxisManipulation = false;
            };

            model.Axes.Add(xAxis);

            ApplyThemeToPlotModel(model);
            return model;
        }

        private void ApplySeriesStyleToCurrentPlot()
        {
            if (CurrentPlotModel == null) return;

            foreach (var series in CurrentPlotModel.Series.OfType<LineSeries>())
            {
                if (series.LineStyle == LineStyle.Dash) continue;

                if (_isSeriesDottedChecked)
                {
                    series.LineStyle = LineStyle.None;
                    series.MarkerType = MarkerType.Circle;
                    series.MarkerSize = 3;
                    series.MarkerFill = series.Color;
                }
                else
                {
                    series.LineStyle = LineStyle.Solid;
                    series.MarkerType = MarkerType.None;
                }
            }

            CurrentPlotModel.InvalidatePlot(false);
        }

        private PlotModel BuildBetaPlotModel()
        {
            IsYAxisModeEnabled = false;
            IsDatePickerEnabled = false;

            var model = new PlotModel
            {
                PlotType = PlotType.XY,
                Title = _localizationService.GetString("Str_BetaEstimation"),
                Subtitle = $"Beta: {_beta.Beta:0.00000000} ± {_beta.SigmaBeta:0.00000000}",
            };

            var scatter = new ScatterSeries { 
                MarkerType = MarkerType.Circle, 
                MarkerSize = 3, 
                MarkerFill = OxyColors.CadetBlue,
                TrackerFormatString =  "P-P0 (hPa): {X:0.00}\nln(N): {Y:0.0000}"
            };

            for (int i = 0; i < _data.PressureMinusRef.Count; i++)
            {
                scatter.Points.Add(new ScatterPoint(_data.PressureMinusRef[i], Math.Log(_data.RawCounts[i])));
            }
            model.Series.Add(scatter);

            var line = new LineSeries { Color = OxyColors.Red, LineStyle = LineStyle.Dash, StrokeThickness = 4 };
            double minX = _data.PressureMinusRef.Min();
            double maxX = _data.PressureMinusRef.Max();
            double diffX = Math.Abs(minX - maxX);
            double q = Math.Log(_data.RawCounts.Average()) - (_beta.Beta * _data.PressureMinusRef.Average()); // simplified intercept
            line.Points.Add(new DataPoint(minX, minX * _beta.Beta + q));
            line.Points.Add(new DataPoint(maxX, maxX * _beta.Beta + q));
            model.Series.Add(line);

            double minY = scatter.Points.Min(p => p.Y);
            double maxY = scatter.Points.Max(p => p.Y);
            double diffY = Math.Abs(minY - maxY);

            var axisX = new LinearAxis { 
                Position = AxisPosition.Bottom, 
                MajorGridlineStyle = LineStyle.Solid,
                Title = "P - P0 (hPa)",
                AbsoluteMaximum = maxX + diffX,
                AbsoluteMinimum = minX - diffX,
                Minimum = minX - 0.2 * diffX,
                Maximum = maxX + 0.2 * diffX,
            };
            axisX.AxisChanged += (s, e) =>
            { 
                IsResetButtonVisible = true; 
                IsStretchButtonVisible = false; 
            };

            model.Axes.Add(axisX);

            var axisY = new LinearAxis { 
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                Title = "Ln(N)",
                AbsoluteMaximum = maxY + diffY,
                AbsoluteMinimum = minY - diffY,
                Maximum = maxY + 0.2 * diffY,
                Minimum = minY - 0.2 * diffY,
            };
            axisY.AxisChanged += (s, e) => 
            { 
                IsResetButtonVisible = true; 
                IsStretchButtonVisible = false;
            };

            model.Axes.Add(axisY);

            ApplyThemeToPlotModel(model);
            return model;
        }

        private void UpdateSmoothAnnotation()
        {
            if (CurrentPlotModel == null) return;
            
            var series = CurrentPlotModel.Series.FirstOrDefault(s => s is LineSeries ls && ls.LineStyle != LineStyle.Dash) as LineSeries;
            if (series != null)
            {
                if (_smoothAnnotation != null) CurrentPlotModel.Annotations.Remove(_smoothAnnotation);
                
                if (AvgSliderValue > 0)
                {
                    _smoothAnnotation = CreateSmoothedAnnotation(series.Points.ToList(), (uint)AvgSliderValue, _confareaColor);
                    CurrentPlotModel.Annotations.Add(_smoothAnnotation);
                }
                CurrentPlotModel.InvalidatePlot(false);
            }
        }

        private PolylineAnnotation CreateSmoothedAnnotation(List<DataPoint> dp, uint window, OxyColor color)
        {
            var res = new PolylineAnnotation
            {
                Color = OxyColors.Red,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 2,
                LineJoin = LineJoin.Round,
            };

            List<DateTime> dateGaps = _dataCorrection.GetDateGaps();

            for (int i = 0; i < dp.Count; i++)
            {
                double sumY = 0;
                double sumWeights = 0;

                int start = Math.Max(0, i - (int)window);
                int end = Math.Min(dp.Count - 1, i + (int)window);

                for (int j = start; j <= end; j++)
                {
                    double dist = Math.Abs(i - j);
                    double weight = 1.0 - (dist / (window + 1.0));
                    sumY += dp[j].Y * weight;
                    sumWeights += weight;
                }

                res.Points.Add(new DataPoint(dp[i].X, sumY / sumWeights));
            }

            return res;
        }

        private void ApplyThemeToPlotModel(PlotModel model)
        {
            if (model == null) return;
            
            bool isDark = false;
            if (Application.Current != null)
            {
                var theme = ThemeManager.Current.DetectTheme(Application.Current);
                isDark = theme?.BaseColorScheme == "Dark";
            }
            
            var textColor = isDark ? OxyColors.White : OxyColors.Black;
            var axisLineColor = isDark ? OxyColor.FromArgb(255, 120, 120, 120) : OxyColors.Black;
            var gridLineColor = isDark ? OxyColor.FromArgb(255, 60, 60, 60) : OxyColor.FromArgb(255, 200, 200, 200);
            
            model.TextColor = textColor;
            model.PlotAreaBorderColor = axisLineColor;
            model.LegendTitleColor = textColor;
            model.LegendBorder = axisLineColor;
            model.LegendBackground = isDark ? OxyColor.FromArgb(200, 50, 50, 50) : OxyColor.FromArgb(190, 230, 230, 230);
            model.Background = isDark ? OxyColor.FromRgb(37, 37, 37) : OxyColors.White;
            model.DefaultFont = "Trebuchet MS";

            int intLen = 40;
            if(_doubleGraphActive)
                intLen = 34;
            model.Axes.Last().IntervalLength = _silentDataCorrection ? 20 : intLen; 

            foreach (var axis in model.Axes)
            {
                axis.TextColor = textColor;
                axis.TicklineColor = axisLineColor;
                axis.AxislineColor = axisLineColor;
                axis.TitleColor = textColor;
                if (axis.MajorGridlineStyle != LineStyle.None)
                {
                    axis.MajorGridlineColor = gridLineColor;
                }
                if (axis.MinorGridlineStyle != LineStyle.None)
                {
                    axis.MinorGridlineColor = gridLineColor;
                }
            }
            
            foreach (var s in model.Series)
            {
                if (s is ScatterSeries sc && (sc.MarkerFill == OxyColors.Black || sc.MarkerFill == OxyColors.White))
                {
                    sc.MarkerFill = textColor;
                }
                if (s is LineSeries ls && (ls.Color == OxyColors.Black || ls.Color == OxyColors.White))
                {
                    ls.Color = textColor;
                }
                if (s is LineSeries lsSemi && (lsSemi.Color == OxyColor.FromArgb(128, 0, 0, 0) || lsSemi.Color == OxyColor.FromArgb(128, 255, 255, 255)))
                {
                    lsSemi.Color = isDark ? OxyColor.FromArgb(128, 255, 255, 255) : OxyColor.FromArgb(128, 0, 0, 0);
                }
            }
        }
    }
}
