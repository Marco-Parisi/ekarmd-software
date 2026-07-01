using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;

namespace MuonDetectorReader
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
     
            base.OnStartup(e);

            if (e.Args.Length == 2)
            {
                string path = e.Args[0];
                string detName = e.Args[1];

                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    File.WriteAllText(path + @"\crash_log.txt", args.ExceptionObject.ToString());
                };

                MainWindow tempMW = new MainWindow();
                tempMW.DataProcessingForHFS(path, detName);

                Shutdown();
            }
        }
    }
}
