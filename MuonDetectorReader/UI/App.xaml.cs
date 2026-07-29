using System;
using System.IO;
using System.Windows;
using ControlzEx.Theming;
using MuonDetectorReader.Services;
using MuonDetectorReader.ViewModels;

namespace MuonDetectorReader
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settingsService = new SettingsService();
            var settings = settingsService.Load();
            if (!string.IsNullOrEmpty(settings.Theme))
            {
                ThemeManager.Current.ChangeThemeBaseColor(this, settings.Theme);
            }
            LocalizationService.Current.Initialize();

            if (e.Args.Length >= 2)
            {
                string path = e.Args[0];
                string detName = e.Args[1];
                int days = 14;

                if (e.Args.Length == 3)
                    int.TryParse(e.Args[2], out days);

                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    File.WriteAllText(Path.Combine(path, "crash_log.txt"), args.ExceptionObject.ToString());
                };

                // Use ViewModel directly without creating a Window
                var viewModel = new MainViewModel();
                viewModel.DataProcessingForCLI(path, detName, days);

                Shutdown();
            }
        }
    }
}
