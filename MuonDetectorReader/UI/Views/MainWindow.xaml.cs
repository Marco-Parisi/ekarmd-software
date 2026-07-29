using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Theming;
using MuonDetectorReader.Services;
using MuonDetectorReader.ViewModels;
using OxyPlot;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace MuonDetectorReader
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        // PressBox numeric input validation (pure UI behavior)
        private static readonly Regex _numericRegex = new Regex(@"[^0-9,\,]+");
        private void PressBoxInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _numericRegex.IsMatch(e.Text);
        }
        private void PressBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space || e.Key == Key.Enter)
                e.Handled = true;
        }

        // Show/Hide help text — toggles between plot and message visibility
        private void ShowHelpClick(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                vm.IsPlotVisible = !vm.IsPlotVisible;
                vm.IsMessageVisible = !vm.IsMessageVisible;
            }
        }
        private void ToggleThemeClick(object sender, RoutedEventArgs e)
        {
            var currentTheme = ThemeManager.Current.DetectTheme(Application.Current);
            if (currentTheme != null)
            {
                var newTheme = currentTheme.BaseColorScheme == "Dark" ? "Light" : "Dark";
                ThemeManager.Current.ChangeThemeBaseColor(Application.Current, newTheme);

                var settingsService = new SettingsService();
                var settings = settingsService.Load();
                settings.Theme = newTheme;
                settingsService.Save(settings);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
