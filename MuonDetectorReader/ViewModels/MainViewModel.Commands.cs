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
        #region Commands

        public RelayCommand OpenFileCommand { get; private set; }
        public RelayCommand ToggleLanguageCommand { get; private set; }
        public RelayCommand EstimateBetaCommand { get; private set; }
        public RelayCommand ShowRawCountsCommand { get; private set; }
        public RelayCommand ShowCorrectedCountsCommand { get; private set; }
        public RelayCommand ShowPressureCommand { get; private set; }
        public RelayCommand ShowTemperatureCommand { get; private set; }
        public RelayCommand ShowDoubleGraphPanelCommand { get; private set; }
        public RelayCommand DoubleGraphOkCommand { get; private set; }
        public RelayCommand ExportGraphCommand { get; private set; }
        public RelayCommand ExportFileCommand { get; private set; }
        public RelayCommand ResetGraphCommand { get; private set; }
        public RelayCommand StretchGraphCommand { get; private set; }
        public RelayCommand ShowHelpCommand { get; private set; }
        public RelayCommand ToggleSidebarCommand { get; private set; }

        private void InitializeCommands()
        {
            OpenFileCommand = new RelayCommand(ExecuteOpenFile);
            ToggleLanguageCommand = new RelayCommand(ExecuteToggleLanguage);
            EstimateBetaCommand = new RelayCommand(ExecuteEstimateBeta);
            ShowRawCountsCommand = new RelayCommand(() => ExecuteShowGraphCommand("CG"));
            ShowCorrectedCountsCommand = new RelayCommand(() => ExecuteShowGraphCommand("CC"));
            ShowPressureCommand = new RelayCommand(() => ExecuteShowGraphCommand("Press"));
            ShowTemperatureCommand = new RelayCommand(() => ExecuteShowGraphCommand("Temp"));
            ShowDoubleGraphPanelCommand = new RelayCommand(() => IsDoubleGraphFlyoutOpen = !IsDoubleGraphFlyoutOpen);
            DoubleGraphOkCommand = new RelayCommand(ExecuteDoubleGraphOk);
            ExportGraphCommand = new RelayCommand(ExecuteExportGraph);
            ExportFileCommand = new RelayCommand(ExecuteExportFile);
            ResetGraphCommand = new RelayCommand(ExecuteResetGraph);
            StretchGraphCommand = new RelayCommand(ExecuteStretchGraph);
            ShowHelpCommand = new RelayCommand(ExecuteShowHelp);
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarOnRight = !IsSidebarOnRight);
        }

        #endregion
    }
}
