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
        #region Properties

        private string _windowTitle;
        public string WindowTitle { get => _windowTitle; set => SetProperty(ref _windowTitle, value); }

        private string _minorVersion;
        public string MinorVersion { get => _minorVersion; set => SetProperty(ref _minorVersion, value); }

        private string _betaText;
        public string BetaText { get => _betaText; set => SetProperty(ref _betaText, value); }

        private string _referencePressureText = "1013,25";
        public string ReferencePressureText
        {
            get => _referencePressureText;
            set
            {
                if (SetProperty(ref _referencePressureText, value))
                {
                    if (double.TryParse(value.Replace(".", ","), out double p0))
                    {
                        var s = _settings.Load();
                        s.ReferencePressure = p0;
                        _settings.Save(s);
                        _beta.ReferencePressure = p0;
                    }
                }
            }
        }

        private bool _isBetaPanelEnabled;
        public bool IsBetaPanelEnabled { get => _isBetaPanelEnabled; set => SetProperty(ref _isBetaPanelEnabled, value); }

        private bool _isGraphPanelEnabled;
        public bool IsGraphPanelEnabled { get => _isGraphPanelEnabled; set => SetProperty(ref _isGraphPanelEnabled, value); }

        private bool _isExportPanelEnabled;
        public bool IsExportPanelEnabled { get => _isExportPanelEnabled; set => SetProperty(ref _isExportPanelEnabled, value); }

        private bool _isDatePickerEnabled;
        public bool IsDatePickerEnabled { get => _isDatePickerEnabled; set => SetProperty(ref _isDatePickerEnabled, value); }

        private bool _isOutlierBoxEnabled;
        public bool IsOutlierBoxEnabled { get => _isOutlierBoxEnabled; set => SetProperty(ref _isOutlierBoxEnabled, value); }

        private bool _isOutlierChecked;
        public bool IsOutlierChecked
        {
            get => _isOutlierChecked;
            set
            {
                if (SetProperty(ref _isOutlierChecked, value))
                {
                    IsOutlierSliderEnabled = IsOutlierBoxEnabled && value;
                    if (_data.RawCounts.Count > 0)
                    {
                        PreserveZoomAndExecute(OutlierAction);
                    }
                }
            }
        }

        private bool _isOutlierSliderEnabled;
        public bool IsOutlierSliderEnabled { get => _isOutlierSliderEnabled; set => SetProperty(ref _isOutlierSliderEnabled, value); }

        private double _outlierSigmaValue = 3.0;
        public double OutlierSigmaValue
        {
            get => _outlierSigmaValue;
            set
            {
                if (SetProperty(ref _outlierSigmaValue, value))
                {
                    OutlierSigmaText = value.ToString("0.00") + "σ";
                    if ((_isOutlierChecked || _isYAxisSigma) && _data.RawCounts.Count > 0)
                    {
                        if (_isOutlierChecked) 
                            PreserveZoomAndExecute(OutlierAction);
                        else 
                            PreserveZoomAndExecute(() => ExecuteShowGraph(_activeGraph));
                    }
                }
            }
        }

        private string _outlierSigmaText = "3,00σ";
        public string OutlierSigmaText { get => _outlierSigmaText; set => SetProperty(ref _outlierSigmaText, value); }

        private bool _isYAxisModeEnabled;
        public bool IsYAxisModeEnabled { get => _isYAxisModeEnabled; set => SetProperty(ref _isYAxisModeEnabled, value); }

        private bool _isYAxisPercentage = true;
        public bool IsYAxisPercentage
        {
            get => _isYAxisPercentage;
            set
            {
                if (SetProperty(ref _isYAxisPercentage, value) && value)
                {
                    _isYAxisSigma = false;
                    OnPropertyChanged(nameof(IsYAxisSigma));
                    _isYAxisAbsolute = false;
                    OnPropertyChanged(nameof(IsYAxisAbsolute));

                    ToggleYAxisTypeAction();
                }

            }
        }

        private bool _isYAxisSigma;
        public bool IsYAxisSigma
        {
            get => _isYAxisSigma;
            set
            {
                if (SetProperty(ref _isYAxisSigma, value) && value)
                {
                    _isYAxisPercentage = false;
                    OnPropertyChanged(nameof(IsYAxisPercentage));
                    _isYAxisAbsolute = false;
                    OnPropertyChanged(nameof(IsYAxisAbsolute));

                    ToggleYAxisTypeAction();
                }

            }
        }

        private bool _isYAxisAbsolute;
        public bool IsYAxisAbsolute
        {
            get => _isYAxisAbsolute;
            set
            {
                if (SetProperty(ref _isYAxisAbsolute, value) && value)
                {
                    _isYAxisPercentage = false;
                    OnPropertyChanged(nameof(IsYAxisPercentage));
                    _isYAxisSigma = false;
                    OnPropertyChanged(nameof(IsYAxisSigma));

                    ToggleYAxisTypeAction();
                }

            }
        }

        private bool _isTempCorrEnabled;
        public bool IsTempCorrEnabled { get => _isTempCorrEnabled; set => SetProperty(ref _isTempCorrEnabled, value); }

        private bool _isTempCorrChecked = true;
        public bool IsTempCorrChecked
        {
            get => _isTempCorrChecked;
            set
            {
                if (SetProperty(ref _isTempCorrChecked, value))
                {
                    ExecuteToggleTempCorr();
                }
            }
        }

        private bool _isShowHideDataEnabled;
        public bool IsShowHideDataEnabled { get => _isShowHideDataEnabled; set => SetProperty(ref _isShowHideDataEnabled, value); }

        private bool _isShowHideDataChecked;
        public bool IsShowHideDataChecked
        {
            get => _isShowHideDataChecked;
            set
            {
                if (SetProperty(ref _isShowHideDataChecked, value))
                {
                    ExecuteToggleShowHideData();
                }
            }
        }
        private bool _isSeriesDottedChecked;
        public bool IsSeriesDottedChecked
        {
            get => _isSeriesDottedChecked;
            set
            {
                if (SetProperty(ref _isSeriesDottedChecked, value))
                {
                    OnPropertyChanged(nameof(ChartModeText));
                    var s = _settings.Load();
                    s.IsSeriesDottedChecked = value;
                    _settings.Save(s);

                    ApplySeriesStyleToCurrentPlot();
                }
            }
        }

        public string ChartModeText => _isSeriesDottedChecked
            ? _localizationService.GetString("Str_ChartMode_Points")
            : _localizationService.GetString("Str_ChartMode_Line");

        private bool _isAvgSliderEnabled;
        public bool IsAvgSliderEnabled { get => _isAvgSliderEnabled; set => SetProperty(ref _isAvgSliderEnabled, value); }

        private double _avgSliderValue = 6.0;
        public double AvgSliderValue
        {
            get => _avgSliderValue;
            set
            {
                if (SetProperty(ref _avgSliderValue, value))
                {
                    SmoothValueText = value == 0 ? "OFF" : value.ToString() + "pt";
                    IsShowHideDataEnabled = value > 0;
                    if (value == 0) IsShowHideDataChecked = false;

                    if (CurrentPlotModel != null && _data.RawCounts.Count > 0)
                        UpdateSmoothAnnotation();
                }
            }
        }

        private string _smoothValueText = "6pt";
        public string SmoothValueText { get => _smoothValueText; set => SetProperty(ref _smoothValueText, value); }

        private DateTime? _dateFromOld;
        private DateTime? _dateFrom;
        public DateTime? DateFrom
        {
            get => _dateFrom;
            set
            {
                if (SetProperty(ref _dateFrom, value))
                {
                    if (value.HasValue && CurrentPlotModel != null)
                    {
                        if (DateFrom.Value != _dateFromOld)
                        {
                            if (DateTo != null)
                            {
                                if (value >= DateTo.Value)
                                {
                                    _messageBox.ShowAsync(_localizationService.GetString("Str_DateRangeError"), _localizationService.GetString("Str_Warning"));
                                    DateFrom = _dateFromOld;
                                    return;
                                }
                            }

                            var xAxis = CurrentPlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
                            if (xAxis != null && !_xAxisManipulation)
                            {
                                xAxis.Minimum = DateTimeAxis.ToDouble(value.Value);
                                xAxis.Reset();

                                CurrentPlotModel.InvalidatePlot(false);
                                IsResetButtonVisible = true;
                            }
                            _dateFromOld = value;
                        }
                    }
                }
            }
        }

        private DateTime? _dateToOld;
        private DateTime? _dateTo;
        public DateTime? DateTo
        {
            get => _dateTo;
            set
            {
                if (SetProperty(ref _dateTo, value))
                {
                    if (value.HasValue && CurrentPlotModel != null)
                    {
                        if (DateTo.Value != _dateToOld)
                        {
                            if (DateFrom != null)
                            {
                                if (value <= DateFrom.Value)
                                {
                                    _messageBox.ShowAsync(_localizationService.GetString("Str_DateRangeError"), _localizationService.GetString("Str_Warning"));
                                    DateTo = _dateToOld;

                                    return;
                                }
                            }

                            var xAxis = CurrentPlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
                            if (xAxis != null && !_xAxisManipulation)
                            {
                                xAxis.Maximum = DateTimeAxis.ToDouble(value.Value);
                                xAxis.Reset();

                                CurrentPlotModel.InvalidatePlot(false);
                                IsResetButtonVisible = true;
                            }

                            _dateToOld = value;
                        }
                    }
                }
            }
        }

        private DateTime? _dateMinimum;
        public DateTime? DateMinimum { get => _dateMinimum; set => SetProperty(ref _dateMinimum, value); }

        private DateTime? _dateMaximum;
        public DateTime? DateMaximum { get => _dateMaximum; set => SetProperty(ref _dateMaximum, value); }

        private DateTime? _dateFromMaximum;
        public DateTime? DateFromMaximum { get => _dateFromMaximum; set => SetProperty(ref _dateFromMaximum, value); }

        private DateTime? _dateToMinimum;
        public DateTime? DateToMinimum { get => _dateToMinimum; set => SetProperty(ref _dateToMinimum, value); }

        private bool _isDoubleGraphFlyoutOpen;
        public bool IsDoubleGraphFlyoutOpen { get => _isDoubleGraphFlyoutOpen; set 
            { 
                SetProperty(ref _isDoubleGraphFlyoutOpen, value);

                if (value)
                    BlurHelper.BlurShow();
                else
                    BlurHelper.BlurHide();
            } 
        }

        private bool _isDgCgChecked;
        public bool IsDgCgChecked { get => _isDgCgChecked; set { if (SetProperty(ref _isDgCgChecked, value) && value) { IsDgCcpChecked = false; IsDgCcptChecked = false; } } }

        private bool _isDgCcpChecked;
        public bool IsDgCcpChecked { get => _isDgCcpChecked; set { if (SetProperty(ref _isDgCcpChecked, value) && value) { IsDgCgChecked = false; IsDgCcptChecked = false; } } }

        private bool _isDgCcptChecked;
        public bool IsDgCcptChecked { get => _isDgCcptChecked; set { if (SetProperty(ref _isDgCcptChecked, value) && value) { IsDgCgChecked = false; IsDgCcpChecked = false; } } }

        private bool _isDgPChecked;
        public bool IsDgPChecked { get => _isDgPChecked; set { if (SetProperty(ref _isDgPChecked, value) && value) { IsDgTChecked = false; } } }

        private bool _isDgTChecked;
        public bool IsDgTChecked { get => _isDgTChecked; set { if (SetProperty(ref _isDgTChecked, value) && value) { IsDgPChecked = false; } } }

        private PlotModel _currentPlotModel;
        public PlotModel CurrentPlotModel
        {
            get => _currentPlotModel;
            set
            {
                if (SetProperty(ref _currentPlotModel, value))
                {
                    if (_currentPlotModel != null)
                    {
                        _initialAxisBounds = new Dictionary<Axis, (double Min, double Max)>();
                        foreach (var axis in _currentPlotModel.Axes)
                        {
                            _initialAxisBounds[axis] = (axis.Minimum, axis.Maximum);
                        }
                    }
                    else
                    {
                        _initialAxisBounds = null;
                    }
                }
            }
        }

        private PlotController _plotController;
        public PlotController PlotController { get => _plotController; set => SetProperty(ref _plotController, value); }

        private bool _isResetButtonVisible;
        public bool IsResetButtonVisible { get => _isResetButtonVisible; set => SetProperty(ref _isResetButtonVisible, value); }

        private bool _isStretchButtonVisible;
        public bool IsStretchButtonVisible 
        { 
            get => _isStretchButtonVisible; 
            set => SetProperty(ref _isStretchButtonVisible, value); 
        }

        private double _yAxisMajorStep = double.NaN;
        private bool _isYAxisChanged;
        private bool _isPlotVisible;
        public bool IsPlotVisible { get => _isPlotVisible; set => SetProperty(ref _isPlotVisible, value); }

        private bool _isMessageVisible = true;
        public bool IsMessageVisible { get => _isMessageVisible; set => SetProperty(ref _isMessageVisible, value); }

        private bool _isSidebarOnRight = false;
        public bool IsSidebarOnRight
        {
            get => _isSidebarOnRight;
            set
            {
                if (SetProperty(ref _isSidebarOnRight, value))
                {
                    OnPropertyChanged(nameof(SidebarColumn));
                    OnPropertyChanged(nameof(ContentColumn));
                    OnPropertyChanged(nameof(LeftColumnWidth));
                    OnPropertyChanged(nameof(RightColumnWidth));
                    OnPropertyChanged(nameof(ToggleSidebarIcon));
                    OnPropertyChanged(nameof(ToggleSidebarTooltip));
                    OnPropertyChanged(nameof(SidebarBorderThickness));

                    var s = _settings.Load();
                    s.IsSidebarOnRight = value;
                    _settings.Save(s);
                }
            }
        }

        public int SidebarColumn => _isSidebarOnRight ? 1 : 0;
        public int ContentColumn => _isSidebarOnRight ? 0 : 1;
        public GridLength LeftColumnWidth => _isSidebarOnRight ? new GridLength(1, GridUnitType.Star) : new GridLength(220);
        public GridLength RightColumnWidth => _isSidebarOnRight ? new GridLength(220) : new GridLength(1, GridUnitType.Star);
        public string ToggleSidebarIcon => _isSidebarOnRight ? "🡄" : "🡆";
        public string ToggleSidebarTooltip => _isSidebarOnRight ? _localizationService.GetString("Str_MoveSidebarLeft") : _localizationService.GetString("Str_MoveSidebarRight");
        public Thickness SidebarBorderThickness => _isSidebarOnRight ? new Thickness(1, 0, 0, 0) : new Thickness(0, 0, 1, 0);

        #endregion
    }
}
