//#define KT_ESTIMATION

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
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IFileParserService _fileParser;
        private readonly IDataCorrectionService _dataCorrection;
        private readonly IBetaCalculationService _betaCalculation;
        private readonly IRegressionService _regressionService;
        private readonly ISettingsService _settings;
        private readonly IExportService _export;
        private readonly IFileDialogService _fileDialog;
        private readonly IMessageBoxService _messageBox;
        private readonly IkTCalculationService _ktCalculation;
        private readonly ILocalizationService _localizationService;

        private DetectorDataSet _data;
        private BetaEstimation _beta;
        private KTEstimation _kt;

        private string _activeGraph = "";
        private bool _doubleGraphActive = false;
        private bool _xAxisManipulation = false;
        private string _xAxisStringFormat = "yyyy/MM/dd";
        private string _plotTitle;
        private bool _silentDataCorrection = false;
        private string _silentDataCorrDays = "0";
        private OxyColor _confareaColor = OxyColors.Black;
        private bool _confAreaIsVisible = false;
        private int _confAreaDaysMin = 7;
        private PolygonAnnotation _confidenceArea = null;
        private PolylineAnnotation _smoothAnnotation = null;

        private Color CGcolor = Color.FromArgb(180, 0, 120, 0);
        private Color CCcolor = Color.FromArgb(200, 50, 110, 200);
        private Color Pcolor = Colors.Orange;
        private Color Tcolor = Colors.Magenta;

        private Dictionary<Axis, (double Min, double Max)> _initialAxisBounds;

        public MainViewModel()
        {
            _fileParser = new FileParserService();
            _dataCorrection = new DataCorrectionService();
            _regressionService = new RegressionService();
            _betaCalculation = new BetaCalculationService(_regressionService);
            _ktCalculation = new kTCalculationService(_regressionService);
            _settings = new SettingsService();
            _export = new ExportService();
            _fileDialog = new FileDialogService();
            _messageBox = new MessageBoxService();
            _localizationService = LocalizationService.Current;
            _localizationService.LanguageChanged += OnLanguageChanged;

            _data = new DetectorDataSet();
            _beta = BetaEstimation.Empty;

            var settings = _settings.Load();
            _referencePressureText = settings.ReferencePressure.ToString("0.##").Replace('.', ',');
            _beta = new BetaEstimation { Beta = settings.Beta, SigmaBeta = settings.SigmaBeta, ReferencePressure = settings.ReferencePressure };
            _kt = new KTEstimation { KT = settings.KT, SigmaKT = settings.SigmaKT };
            _betaText = _beta.Beta == 0 ? _localizationService.GetString("Str_None") : _beta.Beta.ToString("0.000000");
            IsSidebarOnRight = settings.IsSidebarOnRight;
            _isSeriesDottedChecked = settings.IsSeriesDottedChecked;

            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //version = version.Replace(".", "").Insert(1, ".");

            //int minor = 0;
            //int.TryParse(version.Split('.')[1], out minor);

            //version = version.Split('.')[0] + '.' + minor.ToString();
            WindowTitle = "Muon Detector Reader v2";// + version;
            MinorVersion = version.Remove(0, 1);

            InitializeCommands();
            InitializePlotController();

            ThemeManager.Current.ThemeChanged += (sender, e) =>
            {
                if (CurrentPlotModel != null)
                {
                    ApplyThemeToPlotModel(CurrentPlotModel);
                    CurrentPlotModel.InvalidatePlot(false);
                }
            };

            CGcolor = ((SolidColorBrush)Application.Current.FindResource("SidebarGraphRawBrush")).Color;
            CCcolor = ((SolidColorBrush)Application.Current.FindResource("SidebarGraphCorrectedBrush")).Color;
            Pcolor = ((SolidColorBrush)Application.Current.FindResource("SidebarGraphPressureBrush")).Color;
            Tcolor = ((SolidColorBrush)Application.Current.FindResource("SidebarGraphTemperatureBrush")).Color;
        }
    }
}
