using Microsoft.Win32;
using MuonDetectorReader.Utils;
using OxyPlot;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Xml;
using Xceed.Wpf.AvalonDock.Controls;
using static MuonDetectorReader.Graph;
using DateTimePicker = Xceed.Wpf.Toolkit.DateTimePicker;
using FontWeights = System.Windows.FontWeights;
using LineSeries = OxyPlot.Series.LineSeries;

namespace MuonDetectorReader
{ 

    public partial class MainWindow : Window
    {

        List<double> Temp = new List<double>();
        List<double> Press = new List<double>();
        List<double> RawCounts = new List<double>();
        List<double> oldRawCounts = new List<double>();
        List<DateTime> Dates = new List<DateTime>();
        List<double> CorrCounts = new List<double>();
        List<double> FullCorrCounts = new List<double>();
        List<double> DeltaCorrCounts = new List<double>();
        List<double> DeltaFullCorrCounts = new List<double>();

        private static readonly Regex charRegex = new Regex("[a-z]+");

        double Temp_avg;
        const double kT = -0.00170065228125; // Valido solo per EKAR, stimato dai dati 2024_09 - 2025_11
        const double SigmakT = 1.3346453895E-18;

        public static string GraphTitle = "Grafico";
        public static string ActiveGraph = "";

        string SettingFilePath;

        public static string FileName;
        public static bool HideData;

        public static bool SilentDataCorrection = false;
        public static string SilentDataCorrDays;
        public MainWindow()
        {
            InitializeComponent();

            SettingFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CRD1 Reader\\Settings.xml");
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            version = version.Replace(".", "").Insert(1, ".");
            WindowTitle.Text = "Muon Detector Reader v" + version;
            OpenSettingsFile();
        }

        private void PressBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender != null)
            {
                try
                {
                    string str = (sender as TextBox).Text;
                    double numstr = Convert.ToDouble(str);

                    AddPressToSettingsFile();
                }
                catch {}
            }
        }
        private void PressBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Space) || e.Key.Equals(Key.Enter))
            {
                e.Handled = true;
            }
        }
        private static readonly Regex _regex = new Regex(@"[^0-9,\,]+");
        private void PressBoxInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _regex.IsMatch(e.Text);
        }

        public void DataProcessingForHFS(string HFSpath, string DetectorName, int days)
        {
            SilentDataCorrection = true;
            SilentDataCorrDays = days.ToString();

            DateTime date = DateTime.Now;
            string year = date.ToString("yyyy");
            List<string> files = Directory.GetFiles(HFSpath, "CoolTerm Capture *" + year + "*.txt").ToList();
            files.Reverse();

            string outfilename = Path.Combine(HFSpath, "merge", $"{DetectorName} {year}.txt");
            
            if (files.Count > 0)
            {
                List<string> outLines = new List<string>();
                HashSet<string> knownLines = new HashSet<string>();

                if (File.Exists(outfilename))
                {
                    outLines = File.ReadAllLines(outfilename).ToList();
                    knownLines = new HashSet<string>(outLines);
                }

                foreach (var file in files)
                {
                    foreach (var line in File.ReadLines(file))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // pulizia della stringa da caratteri non numerici
                        string cleanedLine = charRegex.Replace(line, "");

                        // controllo sull'HashSet
                        if (!knownLines.Contains(cleanedLine))
                        {
                            outLines.Add(cleanedLine);
                            knownLines.Add(cleanedLine);
                        }
                    }
                }

                // 1. separazione righe che non iniziano con un numero (l[4] perché la riga inizia con 3 spazi)
                List<string> headers = outLines.Where(l => l.Length > 0 && !char.IsDigit(l[4])).ToList();

                // 2. separazione righe che iniziano con l'anno
                List<string> dataRows = outLines.Where(l => l.Length > 0 && char.IsDigit(l[4])).ToList();

                // 3. ordinamento alfabetico
                dataRows.Sort();

                // 4. concatenamento intestazioni e dati ordinati
                outLines = headers.Concat(dataRows).ToList();

                File.WriteAllLines(outfilename, outLines);
            }

            Open_Click(outfilename, null);
            //AvgSlider.Value = 6;
            DateTime dateFrom = Dates.Count > 336 ? date - new TimeSpan(days, 0, 0, 0) : Dates.Last();
            HFSpath += @"\img\";
            string[] graphs = { "counts.png", "pressure.png", "temp.png", "rawcount_pressure.png" };
            int i = 0;
            foreach ( string graph in graphs)
            {
                DateFrom_SelectedDateChanged(new DateTimePicker() { Value = dateFrom }, null);
                DateTo_SelectedDateChanged(new DateTimePicker() { Value = Dates.First() }, null);
                ExportGraph_Click(HFSpath + graph, null);
                DateToOldSD = new DateTime();
                if( i < 2 )
                    Graph_Click(new Button() { Tag = i>0 ? "Temp" : "Press" }, null);
                else
                {
                    DG_CG.IsChecked = DG_P.IsChecked = true;
                    DoubleGraphOK_Click(null, null);
                }
                i++;
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {

            string path = SilentDataCorrection ? (string)sender : null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "file txt o csv |*.txt;*.csv";
            bool dialogResult = SilentDataCorrection ? true : openFileDialog.ShowDialog().Value;

            if (dialogResult || SilentDataCorrection)
            {
                if(!SilentDataCorrection)
                    path = openFileDialog.FileName;

                try
                {
                    if (MainGrid != null)
                    {
                        if (MainGrid.Children.Count >= 4)
                            MainGrid.Children.RemoveAt(3);
                    }

                    ParseTXT(path);
                    FileName = path.Remove(0, path.LastIndexOf("\\") + 1);

                    Dates.Reverse();
                    Temp.Reverse();
                    Press.Reverse();
                    RawCounts.Reverse();

                    RemoveDuplicates();

                    oldRawCounts = new List<double>(RawCounts);

                    if (BetaBox.Text == "nessuno" || BetaBox.Text == "0,000000")
                    {
                        MessageBox.Show("Per continuare:\n 1) Inserire un valore valido di P0 (Pressione di riferimento).\n 2) cliccare su \"Stima Beta\".", "Attenzione");
                    }
                    else if (BetaBox.Text != "nessuno" && double.TryParse(PressBox.Text, out double refPress))
                    {
                        if (!SilentDataCorrection)
                            DataLostCheck();

                        PmP0.Clear();
                        Press.ForEach(point => PmP0.Add(point - refPress));

                        GenerateCorrCounts();
                        GraficiPanel.IsEnabled = true;
                        ShowHideData.IsChecked = HideData = false;

                        DateTo.Value = DateFrom.Value = null;

                        DateTo.Minimum = DateFrom.Minimum = Dates.Last();
                        DateTo.Maximum = DateFrom.Maximum = Dates.First();

                        Graph_Click(new Button() { Tag = "CC" }, null);

                        //DateTo.DisplayDateStart = DateFrom.DisplayDateStart = Dates.Last();
                        //DateTo.DisplayDateEnd = DateFrom.DisplayDateEnd = Dates.First();

                        //DatePickerResetDate();

                    }

                    BetaPanel.IsEnabled = true;
                    ShowHideData.IsEnabled = true;

                }
                catch
                {

                    BetaPanel.IsEnabled = false;
                    GraficiPanel.IsEnabled = false;
                    ExportPanel.IsEnabled = false;
                    ShowHideData.IsEnabled = false;

                    MessageBox.Show("Formato dei dati incompatibile", "Errore");
                }
            }
            //else
            //{
            //    MessageTextblock.Text = "Errore : File non supportato.";
            //    //MessageBox.Show("File non supportato", "Errore");
            //}

        }

        private Task DataLostCheck()
        {   
            TimeSpan cadence = new TimeSpan(1, 0, 0); // Tempo di integrazione 1 ora
            TimeSpan timediff = new TimeSpan();

            List<string> LostData = new List<string>();

            for (int i = 0; i < Dates.Count - 1; i++)
            {
                timediff = Dates[i] - Dates[i + 1];
                if (timediff > cadence)
                {
                    if(Dates[i].Date - Dates[i + 1].Date == new TimeSpan())
                        LostData.Add("• Il " + Dates[i + 1].ToString("dd/MM/yyyy 'dalle ore' HH:mm") + Dates[i].ToString(" 'alle ore' HH:mm"));
                    else
                        LostData.Add("• Il " + Dates[i + 1].ToString("dd/MM/yyyy 'ore' HH:mm") + " e il " + Dates[i].ToString("dd/MM/yyyy 'ore' HH:mm"));
                    LostData[LostData.Count - 1] += " → " + (timediff.TotalHours-1).ToString() + " ore";
                }
            }

            if(LostData.Count > 0) 
            {
                string LostedData = "Sono stati trovati dei dati mancanti:\n\n";

                foreach (string str in LostData) 
                    LostedData += str + "\n";

                LostedData += "\nNome del file:\n" + FileName;

                Task.Factory.StartNew( () => MessageBox.Show(LostedData, "Attenzione"));
            }

            return Task.CompletedTask;
        }

        private void ExportGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string date = DateTime.Now.ToString("dd'-'MM-yyyy' 'HH'h'mm'm'ss's'");
                string folder;
                int graphWidth = 1600, graphHeight = 900;

                if (!SilentDataCorrection)
                {
                    path += "\\Moun Detector Reader - Grafici";

                    Directory.CreateDirectory(path);
                    folder = path;
                    path += "\\" + GraphTitle + " - " + date + ".png";
                }
                else
                {
                    path = (string)sender;
                    folder = path.Replace(path.Split('\\').Last(),"");
                    graphWidth = 900;
                    graphHeight = 500;
                    
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    (((MainGrid.Children[3] as Grid).Children[1] as StackPanel).Children[1] as Button).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }

                PngExporter pngExporter = new PngExporter { Width = graphWidth, Height = graphHeight, Background = OxyColors.White };


                PlotModel pm = ((MainGrid.Children[3] as Grid).Children[0] as PlotView).Model;
                pngExporter.ExportToFile(pm, path);


                if (!SilentDataCorrection) { 
                    MessageBox.Show("Grafico esportato correttamente nella cartella \"Moun Detector Reader - Grafici\" sul Desktop.\n \n" + path, "Esporta Grafico");

                    Process.Start(folder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source);
            }
        }

        private void ExportFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string date = DateTime.Now.ToString("dd'-'MM-yyyy' 'HH'h'mm'm'ss's'");
                string folder;
                path += "\\Moun Detector Reader - Grafici";
                Directory.CreateDirectory(path);
                folder = path;
                path += "\\" + "Moun Detector Reader Data Export - " + date + ".txt";

                StreamWriter sw = new StreamWriter(path);

                sw.WriteLine("### Export dei dati: P0 = {0} ; Beta = {1} ± {2}", refPress, Beta, SigmaBeta);
                sw.WriteLine("### Coeff. Corr. Temperatura (EKAR): kT = {0} ± {1}", kT, SigmakT);

                sw.Write("### Rimozione Outliers: ");
                if (OutlierBox.IsChecked == true)
                    sw.WriteLine("Attiva (" + OutlierSlider.Value.ToString("F") + "σ)");
                else
                    sw.WriteLine("Disattiva");

                sw.WriteLine("### File di origine => " + FileName);
                sw.WriteLine("### PCC = Pressure Corrected Counts " + FileName);
                sw.WriteLine("### PTCC = Pressure Temperature Corrected Counts " + FileName);
                sw.WriteLine("\n### Formato dei dati separati da TAB:\n");
                sw.WriteLine("Date\tTemp\tPress\tCounts\tPCC\tErr PCC\tPTCC\tErr PTCC");
                sw.WriteLine();

                int i = CorrCounts.Count - 1;
                for (; i >= 0; i--)
                    sw.WriteLine(Dates[i] + "\t{0:.0}\t{1:.00}\t{2}\t{3:.00}\t{4:.00}\t{5:.00}\t{6:.00}", 
                        Temp[i], 
                        Press[i], 
                        RawCounts[i], 
                        Math.Round(CorrCounts[i], 2),
                        Math.Round(DeltaCorrCounts[i], 2),
                        Math.Round(FullCorrCounts[i], 2),
                        Math.Round(DeltaFullCorrCounts[i], 2));

                sw.Close();

                MessageBox.Show("File esportato correttamente nella cartella \"Moun Detector Reader - Grafici\" sul Desktop.\n \n" + path, "Esporta File");

                System.Diagnostics.Process.Start(folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source);
            }
        }

        private void Graph_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as Button).Tag.ToString();

            if (MainGrid.Children.Count >= 4)
                MainGrid.Children.RemoveAt(3);

            TempCorrBox.IsEnabled = false;
            OutlierBox.IsEnabled = false;

            if(OutlierBox.IsChecked == true)
                OutlierSlider.IsEnabled = true;
            else
                OutlierSlider.IsEnabled = false;

            DG_CG.IsChecked = DG_CCP.IsChecked = DG_CCPT.IsChecked = false;

            switch (tag)
            {
                case "Beta":
                    if (double.TryParse(PressBox.Text, out double p0))
                    {
                        try
                        {
                            GraphTitle = "Stima di Beta";
                            MainGrid.Children.Add(CalcBeta(Press, RawCounts, p0));
                            GenerateCorrCounts();
                            GraficiPanel.IsEnabled = true;
                            ShowHideData.IsEnabled = false;

                            BetaBox.Text = Beta.ToString("0.000000");
                            AddBetaToSettingsFile();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Errore nel calcolo di Beta, impossibile continuare.");
                            Close();
                        }
                    }
                    else
                        MessageBox.Show("Inserire un valore valido di Pressione");
                    OutlierBox.IsEnabled = true;
                    DatePickerPanel.IsEnabled = AvgSlider.IsEnabled = false;
                    break;
                case "CG":
                    GraphTitle = "Conteggi Grezzi";
                    MainGrid.Children.Add(GraphData(Dates, RawCounts, Color.FromArgb(180, 0, 120, 0), "Conteggi", SmoothValue.Text == "OFF"? false : true, (uint)AvgSlider.Value));;
                    OutlierBox.IsEnabled = DatePickerPanel.IsEnabled = AvgSlider.IsEnabled = true;
                    if((uint)AvgSlider.Value != AvgSlider.Minimum)
                        ShowHideData.IsEnabled = true;
                    
                    break;
                case "CC":
                    if (TempCorrBox.IsChecked == false)
                    {
                        GraphTitle = "Conteggi Corretti in Pressione";
                        MainGrid.Children.Add(GraphData(Dates, CorrCounts, Color.FromArgb(200, 50, 110, 200), "Conteggi Corretti", SmoothValue.Text == "OFF" ? false : true, (uint)AvgSlider.Value, err_data1: DeltaCorrCounts,  HorLine: false));
                    }
                    else
                    {
                        GraphTitle = "Conteggi Corretti in Pressione e Temperatura";
                        MainGrid.Children.Add(GraphData(Dates, FullCorrCounts, Color.FromArgb(200, 50, 110, 200), "Conteggi Corretti", SmoothValue.Text == "OFF" ? false : true, (uint)AvgSlider.Value, err_data1: DeltaFullCorrCounts, HorLine: false, dot: SilentDataCorrection? true : false));
                    }

                    OutlierBox.IsEnabled = DatePickerPanel.IsEnabled = TempCorrBox.IsEnabled = AvgSlider.IsEnabled = true;
                    if ((uint)AvgSlider.Value != AvgSlider.Minimum)
                        ShowHideData.IsEnabled = true;
                    break;
                case "SIGMA":
                    GraphTitle = "Scarto dei Conteggi Corr. in Pressione" + (TempCorrBox.IsChecked.Value ? " e Temperatura" : "");
                    MainGrid.Children.Add(SigmaTwo(Dates, TempCorrBox.IsChecked.Value ? FullCorrCounts : CorrCounts));
                    TempCorrBox.IsEnabled = OutlierBox.IsEnabled = DatePickerPanel.IsEnabled = true;
                    AvgSlider.IsEnabled = false;
                    ShowHideData.IsEnabled = false;
                    break;
                case "Temp":
                    DG_T.IsChecked = true;
                    DoubleGraphOK_Click(null, null);
                    DG_T.IsChecked = false;
                    break;
                case "Press":
                    DG_P.IsChecked = true;
                    DoubleGraphOK_Click(null, null);
                    DG_P.IsChecked = false;
                    break;
                default: return;
            }
            ActiveGraph = tag;
            DatePickerResetDate();

            if (MainGrid.Children.Count >= 4)
            {
                ExportPanel.IsEnabled = true;
                Grid.SetRow(MainGrid.Children[3], 1);
            }
        }

        private void DoubleGraph_Click(object sender, RoutedEventArgs e)
        {
            if (DoubleGraphPanel.Visibility == Visibility.Collapsed)
                DoubleGraphPanel.Visibility = Visibility.Visible;
            else
                DoubleGraphPanel.Visibility = Visibility.Collapsed;
        }

        private void DoubleGraphOK_Click(object sender, RoutedEventArgs e)
        {
            if (DG_P.IsChecked == true || DG_T.IsChecked == true)
            {
                if (MainGrid.Children.Count >= 4)
                    MainGrid.Children.RemoveAt(3);

                TempCorrBox.IsEnabled = false;
                AvgSlider.IsEnabled = false;
                ShowHideData.IsEnabled = false;
                OutlierSlider.IsEnabled = OutlierBox.IsEnabled = false;

                if (DG_CG.IsChecked == true)
                {
                    GraphTitle = DG_P.IsChecked == true ? "ContGrezzi_vs_Press" : "ContGrezzi_vs_Temp";
                    MainGrid.Children.Add(GraphTwoData(Dates, RawCounts, DG_P.IsChecked == true ? Press : Temp, "Conteggi", DG_P.IsChecked == true ? "Pressione (mBar)" : "Temperatura (°C)", Color.FromArgb(180, 0, 120, 0), DG_P.IsChecked == true ? Colors.Orange : Colors.DarkMagenta, data2Div: DG_T.IsChecked == true ? 5 : 50));                   
                }
                else if (DG_CCP.IsChecked == true)
                {
                    GraphTitle = DG_P.IsChecked == true ? "ContCorrettiPress_vs_Press" : "ContCorrettiPress_vs_Temp";
                    MainGrid.Children.Add(GraphTwoData(Dates, CorrCounts, DG_P.IsChecked == true ? Press : Temp, "Conteggi Corretti", DG_P.IsChecked == true ? "Pressione (mBar)" : "Temperatura (°C)", Color.FromArgb(120, 50, 110, 200), DG_P.IsChecked == true ? Colors.Orange : Colors.DarkMagenta, data2Div: DG_T.IsChecked == true ? 5 : 50));
                }
                else if (DG_CCPT.IsChecked == true)
                {
                    GraphTitle = DG_P.IsChecked == true ? "ContCorrettiPressTemp_vs_Press" : "ContCorrettiPressTemp_vs_Temp";
                    MainGrid.Children.Add(GraphTwoData(Dates, FullCorrCounts, DG_P.IsChecked == true ? Press : Temp, "Conteggi Corretti", DG_P.IsChecked == true ? "Pressione (mBar)" : "Temperatura (°C)", Color.FromArgb(120, 50, 110, 200), DG_P.IsChecked == true ? Colors.Orange : Colors.DarkMagenta, data2Div: DG_T.IsChecked == true ? 5 : 50));
                }
                else
                {
                    GraphTitle = DG_P.IsChecked == true ? "Pressione (mBar)" : "Temperatura (°C)";
                    MainGrid.Children.Add(GraphData(Dates, DG_P.IsChecked == true ? Press : Temp, DG_P.IsChecked == true ? Colors.Orange : Colors.DarkMagenta, GraphTitle, false, Div: DG_T.IsChecked == true ? 5 : 50));
                }

                if (MainGrid.Children.Count >= 4)
                {
                    ExportPanel.IsEnabled = true;
                    Grid.SetRow(MainGrid.Children[3], 1);
                }

                DatePickerResetDate();
            }
            else if (DG_CG.IsChecked == true || DG_CCP.IsChecked == true || DG_CCPT.IsChecked == true)
            {
                TempCorrBox.IsChecked = DG_CCPT.IsChecked;
                Graph_Click(new Button() { Tag = DG_CG.IsChecked == true ? "CG" : "CC" }, null);
            }
            
            if (sender != null)
                DoubleGraph_Click(null, null);

        }

        private void TempCorrBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null && MainGrid.Children.Count >= 4 && ActiveGraph != "SIGMA")
            {
                PlotView pv = ((MainGrid.Children[3] as Grid).Children[0] as PlotView);

                OxyPlot.Annotations.PolygonAnnotation confarea = null;
                OxyPlot.Annotations.PolylineAnnotation smoothline = null;

                try
                {
                    confarea = pv.Model.Annotations.OfType<OxyPlot.Annotations.PolygonAnnotation>().First();
                } catch { }
                try
                {
                    smoothline = pv.Model.Annotations.OfType<OxyPlot.Annotations.PolylineAnnotation>().First();
                }catch { }


                if (confarea != null)
                    pv.Model.Annotations.Remove(confarea);

                if (TempCorrBox.IsChecked == true)
                {
                    pv.Model.Title = GraphTitle = "Conteggi Corretti in Pressione e Temperatura";
                    pv.Model.Series.Insert(0, UpdateTemperatureCorr(Dates, FullCorrCounts, pv));

                    confarea = UpdateTemperatureCorrAnn(Dates, FullCorrCounts, DeltaFullCorrCounts, pv);
                }
                else
                {
                    pv.Model.Title = GraphTitle = "Conteggi Corretti in Pressione";
                    pv.Model.Series.Insert(0, UpdateTemperatureCorr(Dates, CorrCounts, pv));

                    confarea = UpdateTemperatureCorrAnn(Dates, CorrCounts, DeltaCorrCounts, pv);
                }
                pv.Model.Series.RemoveAt(1);

                pv.Model.Annotations.Insert(0,confarea);

                List<DataPoint> dp = (pv.Model.Series.First() as LineSeries).Points;

                if (smoothline!=null)
                {
                    pv.Model.Annotations.Remove(smoothline);
                    pv.Model.Annotations.Insert(0, Smoothed(dp, (uint)AvgSlider.Value));
                }

                pv.InvalidatePlot(true);

                if (ShowHideData != null)
                {
                    if (ShowHideData.IsChecked == true)
                        ShowHideData_Click(1, null);
                }
            }

            if (ActiveGraph == "SIGMA")
            {
                Button buttSim = new Button() { Tag = "SIGMA" };
                Graph_Click(buttSim, null);
            }
        }

        private void OutliersBox_Click(object sender, RoutedEventArgs e)
        {
            GenerateCorrCounts();
            Graph_Click(new Button() { Tag = ActiveGraph }, null);

            ShowHideData.IsChecked = HideData = false;

            if (OutlierBox.IsChecked == false)
                OutlierSlider.IsEnabled = false;
            else 
                OutlierSlider.IsEnabled = true;

        }

        private void OutlierSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OutlierSigmaValue.Text = OutlierSlider.Value.ToString("F") + "σ";

            if (OutlierBox.IsChecked == true)
            {
                GenerateCorrCounts();
                Graph_Click(new Button() { Tag = ActiveGraph }, null);
            }
        }

        private void ParseTXT(string path)
        {
            StreamReader sr = new StreamReader(path);
            string str;

            Temp.Clear();
            Press.Clear();
            RawCounts.Clear();
            Dates.Clear();
            CorrCounts.Clear();
            FullCorrCounts.Clear();

            bool csv = path.Contains(".csv");

            do
            {
                str = sr.ReadLine();
                if (csv)
                {
                    if (str.Contains(";"))
                    {
                        str = str.Replace(";", "*");
                        str = str.Replace(",", ".");
                    }
                    else if (str.Contains(","))
                        str = str.Replace(",", "*");
                }
                else
                    str = str.Replace(",", ".");

                if(charRegex.IsMatch(str))
                    str = charRegex.Replace(str, "");

                if (str.Contains("*") && (str.Contains("/") || str.Contains("-")) && !charRegex.IsMatch(str))
                {
                    if (str.Contains("-"))
                        str = str.Replace("-", "/");

                    if (str.Contains("   "))
                        str = str.Replace("   ", "");

                    Dates.Add(Convert.ToDateTime(str.Remove(str.IndexOf("*") - 1)));

                    str = str.Remove(0, str.IndexOf("*") + 1);
                    Temp.Add(Convert.ToDouble(str.Remove(str.IndexOf("*")).Replace(".", ",")));

                    str = str.Remove(0, str.IndexOf("*") + 1);
                    Press.Add(Convert.ToDouble(str.Remove(str.IndexOf("*")).Replace(".", ",")));

                    str = str.Remove(0, str.IndexOf("*") + 1);
                    if (str.Contains(".") || str.Contains(","))
                        RawCounts.Add((int)Convert.ToDouble(str.Replace(".", ",")));
                    else
                        RawCounts.Add(Convert.ToInt32(str));
                }

            } while (!sr.EndOfStream);

            sr.Close();

            if (Dates.Count == 0 || Temp.Count == 0 || Press.Count == 0 || RawCounts.Count == 0)
                throw new Exception("Formato errato");
            
        }


        private void RemoveDuplicates()
        {
            var seen = new HashSet<DateTime>();
            var duplicateIndexes = new List<int>();

            for (int i = 0; i < Dates.Count; i++)
            {
                if (!seen.Add(Dates[i]))
                {
                    duplicateIndexes.Add(i);
                }
            }

            foreach (var index in duplicateIndexes.OrderByDescending(i => i))
            {
                Dates.RemoveAt(index);
                Press.RemoveAt(index);
                Temp.RemoveAt(index);
                RawCounts.RemoveAt(index);
            }
        }

        private void GenerateCorrCounts()
        {
            if (RawCounts.Count != 0 && PmP0.Count != 0)
            {
                if (OutlierBox.IsChecked == true)
                    RawCounts = OutlierRemover.RemoveOutliersSigma(oldRawCounts, OutlierSlider.Value); 
                else
                    RawCounts = new List<double>(oldRawCounts);

                CorrCounts.Clear();
                FullCorrCounts.Clear();

                List<DateTime> DT = new List<DateTime>();
                Dates.ForEach(date => DT.Add(Convert.ToDateTime(date)));

                for (int i = 0; i < PmP0.Count; i++)
                    CorrCounts.Add(Math.Exp(-Beta * PmP0[i])  * RawCounts[i]);

                DeltaCorrCounts = PressureErrorProp();

                Temp_avg = Temp.Average();

                List<double> TmT0 = new List<double>();
                Temp.ForEach(t => TmT0.Add(t - Temp_avg));

                //double kT = CalcTempCoeff(TmT0, RawCounts);

                for (int i = 0; i < CorrCounts.Count; i++)
                  FullCorrCounts.Add(CorrCounts[i] * Math.Exp(-kT * TmT0[i]));

                DeltaFullCorrCounts = TemperatureErrorProp();
            }
        }

        public List<double> PressureErrorProp()
        {
            double DeltaP = 0.5; // hPa

            double N0_bar = RawCounts.Average();

            List<double> Results = new List<double>();

            for (int i = 0; i < CorrCounts.Count; i++)
            {
                double N = CorrCounts[i];
                double P = Press[i];

                double term1 = N0_bar / Math.Pow(N, 2);
                double term2 = Math.Pow(Beta * DeltaP, 2);
                double term3 = Math.Pow((P - refPress) * SigmaBeta, 2);
                double sumUnderRoot = term1 + term2 + term3;

                double deltaN = N * Math.Sqrt(sumUnderRoot);

                Results.Add(deltaN);
            }

            return Results;
        }

        public List<double> TemperatureErrorProp()
        {
            double DeltaT = 0.5; // °C

            double N0_bar = CorrCounts.Average();

            List<double> Results = new List<double>();

            for (int i = 0; i < FullCorrCounts.Count; i++)
            {
                double N = FullCorrCounts[i];
                double T = Temp[i];

                double term1 = N0_bar / Math.Pow(N, 2);
                double term2 = Math.Pow(kT * DeltaT, 2);
                double term3 = Math.Pow((T - Temp_avg) * SigmakT, 2);
                double sumUnderRoot = term1 + term2 + term3;

                double deltaN = N * Math.Sqrt(sumUnderRoot);

                Results.Add(deltaN);
            }

            return Results;
        }


        private void AvgSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender != null && MainGrid.Children.Count >= 4)
            {
                PlotView pv = ((MainGrid.Children[3] as Grid).Children[0] as PlotView);
                List<DataPoint> dp = (pv.Model.Series.First() as LineSeries).Points;

                OxyPlot.Annotations.PolylineAnnotation smoothline = null;

                try
                {
                    smoothline = pv.Model.Annotations.OfType<OxyPlot.Annotations.PolylineAnnotation>().First();
                    if (smoothline != null)
                        pv.Model.Annotations.Remove(smoothline);
                }
                catch { }

                if ((uint)AvgSlider.Value != AvgSlider.Minimum)
                {
                    pv.Model.Annotations.Insert(0, Smoothed(dp, (uint)AvgSlider.Value));


                    SmoothValue.Text = ((uint)AvgSlider.Value).ToString() + "pt";

                    if (ShowHideData != null)
                        if (ShowHideData.IsEnabled == false)
                            ShowHideData.IsEnabled = true;
                }
                else
                {

                    if (ShowHideData != null)
                    {
                        if (ShowHideData.IsChecked == true)
                        {
                            ShowHideData.IsChecked = HideData = false;
                            ShowHideData_Click(1, null);
                        }

                        if (ShowHideData.IsEnabled)
                            ShowHideData.IsEnabled = false;
                    }
                    SmoothValue.Text = "OFF";
                }
                pv.InvalidatePlot(true);
            }
        }

        OxyColor confareaColor = OxyColors.Black;
        private void ShowHideData_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null && MainGrid.Children.Count >= 4)
            {
                HideData = ShowHideData.IsChecked.Value;

                PlotView pv = (MainGrid.Children[3] as Grid).Children[0] as PlotView;
                OxyPlot.Annotations.PolygonAnnotation confarea = null;

                try
                {
                    confarea = pv.Model.Annotations.OfType<OxyPlot.Annotations.PolygonAnnotation>().First();
                }
                catch 
                {
                    pv.Model.Axes[0].Pan(1e-5);
                }

                if (pv.Model.Series.Count > 1)
                  pv.Model.Series[0].IsVisible = !pv.Model.Series[0].IsVisible;

                if (confarea != null)
                {
                    if (confareaColor == OxyColors.Black)
                        confareaColor = confarea.Fill;

                    confarea.Fill = confarea.Fill != OxyColors.Transparent? OxyColors.Transparent : confareaColor;
                }

                pv.InvalidatePlot(true);
            }
        }

        private void OpenSettingsFile()
        {
            if (File.Exists(SettingFilePath))
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(SettingFilePath);

                    PressBox.Text = xmlDoc.SelectSingleNode("Settings/RefPressure").InnerText;
                    refPress = Convert.ToDouble(PressBox.Text);

                    Beta = Convert.ToDouble(xmlDoc.SelectSingleNode("Settings/Beta").InnerText);
                    BetaBox.Text = Beta.ToString("0.000000"); 
                    
                }
                catch
                {
                    File.Delete(SettingFilePath);
                    CreateSettingsFile();
                }
            }
            else
                CreateSettingsFile();
        }

        private void CreateSettingsFile()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement xmlSettings = xmlDoc.CreateElement("Settings");
            xmlDoc.AppendChild(xmlSettings);

            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CRD1 Reader")))
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CRD1 Reader"));

            xmlDoc.Save(SettingFilePath);

            AddPressToSettingsFile();
            AddBetaToSettingsFile();
        }

        private void AddPressToSettingsFile()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(SettingFilePath);

            XmlText xmlRefPresValue = xmlDoc.CreateTextNode(PressBox.Text);
            XmlNode xmlRefPres = xmlDoc.SelectSingleNode("Settings/RefPressure");

            if (xmlRefPres == null)
            {
                XmlNode xmlSettings = xmlDoc.SelectSingleNode("Settings");

                xmlRefPres = xmlDoc.CreateElement("RefPressure");
                xmlRefPresValue = xmlDoc.CreateTextNode(PressBox.Text);

                xmlRefPres.AppendChild(xmlRefPresValue); 
                xmlSettings.AppendChild(xmlRefPres);
            }
            else
            {
                xmlRefPres.InnerText = null;
                xmlRefPres.AppendChild(xmlRefPresValue);
            }
            
            xmlDoc.Save(SettingFilePath);
        }

        private void AddBetaToSettingsFile()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(SettingFilePath);

            XmlText xmlBetaValue = xmlDoc.CreateTextNode(Beta.ToString());
            XmlNode xmlBeta = xmlDoc.SelectSingleNode("Settings/Beta");

            if (xmlBeta == null)
            {
                XmlNode xmlSettings = xmlDoc.SelectSingleNode("Settings");

                xmlBeta = xmlDoc.CreateElement("Beta");
                xmlBetaValue = xmlDoc.CreateTextNode("0");

                xmlBeta.AppendChild(xmlBetaValue);
                xmlSettings.AppendChild(xmlBeta);

            }
            else
            {
                xmlBeta.InnerText = null;
                xmlBeta.AppendChild(xmlBetaValue);
            }

            XmlText xmlSigmaBetaValue = xmlDoc.CreateTextNode(SigmaBeta.ToString());
            XmlNode xmlSigmaBeta = xmlDoc.SelectSingleNode("Settings/SigmaBeta");

            if (xmlSigmaBeta == null)
            {
                XmlNode xmlSettings = xmlDoc.SelectSingleNode("Settings");

                xmlSigmaBeta = xmlDoc.CreateElement("SigmaBeta");
                xmlSigmaBetaValue = xmlDoc.CreateTextNode("0");

                xmlSigmaBeta.AppendChild(xmlSigmaBetaValue);
                xmlSettings.AppendChild(xmlSigmaBeta);

            }
            else
            {
                xmlSigmaBeta.InnerText = null;
                xmlSigmaBeta.AppendChild(xmlSigmaBetaValue);
            }

            xmlDoc.Save(SettingFilePath);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach(var child in ((sender as CheckBox).Parent as StackPanel).Children)
            {
                if((child as CheckBox) != (sender as CheckBox))
                    (child as CheckBox).IsChecked = false;
            }
        }

        private void DatePickerResetDate()
        {
            /*DateFrom.SelectedDateChanged -= DateFrom_SelectedDateChanged;
            DateTo.SelectedDateChanged -= DateTo_SelectedDateChanged;

            DateFrom.SelectedDate = DateFromOldSD = Dates.Last();
            DateTo.SelectedDate = DateToOldSD = Dates.First();

            DateFrom.SelectedDateChanged += DateFrom_SelectedDateChanged;
            DateTo.SelectedDateChanged += DateTo_SelectedDateChanged;*/

            DateFrom.ValueChanged -= DateFrom_SelectedDateChanged;
            DateTo.ValueChanged -= DateTo_SelectedDateChanged;

            DateFrom.Value = DateFromOldSD = Dates.Last();
            DateTo.Value = /*DateToOldSD =*/ Dates.First();

            DateFrom.ValueChanged += DateFrom_SelectedDateChanged;
            DateTo.ValueChanged += DateTo_SelectedDateChanged;
        }


        DateTime DateFromOldSD;
        private void DateFrom_SelectedDateChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender != null && MainGrid.Children.Count >= 4)
            {
                //DateTime SD = (sender as CustomDatePicker).SelectedDate ?? new DateTime();
                DateTime SD = (sender as DateTimePicker).Value ?? new DateTime();

                if (SD > DateTo.Value)//.SelectedDate)
                {
                    MessageBox.Show("Intervallo date errato", "Attenzione");
                    return;
                }

                PlotView pv = ((MainGrid.Children[3] as Grid).Children[0] as PlotView);

                pv.Model.DefaultXAxis.Minimum = OxyPlot.Axes.DateTimeAxis.ToDouble(SD);
                pv.ResetAllAxes();

                int i = 0;
                foreach (Button butt in ((pv.Parent as Grid).Children[1] as StackPanel).Children)
                {
                    butt.Visibility = i > 0 ? Visibility.Visible : Visibility.Collapsed;
                    i++;
                }

                pv.InvalidatePlot(true);

                DateFromOldSD = SD;
            }
        }

        DateTime DateToOldSD;
        private void DateTo_SelectedDateChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender != null && MainGrid.Children.Count >= 4)
            {
                //if (DateTo.SelectedDate != DateToOldSD)

                if (DateTo.Value != DateToOldSD)
                {
                    //DateTime SD = (sender as CustomDatePicker).SelectedDate ?? new DateTime();
                    DateTime SD = (sender as DateTimePicker).Value ?? new DateTime();

                    if (SD < DateFrom.Value)//.SelectedDate)
                    {
                        MessageBox.Show("Intervallo date errato", "Attenzione");
                        //DateTo.SelectedDate = DateToOldSD;
                        DateTo.Value = DateToOldSD;
                        return;
                    }

                    PlotView pv = ((MainGrid.Children[3] as Grid).Children[0] as PlotView);

                    pv.Model.DefaultXAxis.Maximum = OxyPlot.Axes.DateTimeAxis.ToDouble(SD);
                    pv.ResetAllAxes();

                    int i = 0;
                    foreach (Button butt in ((pv.Parent as Grid).Children[1] as StackPanel).Children)
                    {
                        butt.Visibility = i > 0 ? Visibility.Visible : Visibility.Collapsed;
                        i++;
                    }    

                    pv.InvalidatePlot(true);

                    DateToOldSD = SD;
                }
            }
        }

        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowHelpClick(object sender, RoutedEventArgs e)
        {
            if (MainGrid.Children.Count >= 4)
            { 
                var plotVis = (MainGrid.Children[3] as Grid).Visibility;
                var Vis = plotVis == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                (MainGrid.Children[3] as Grid).Visibility = Vis;

                (sender as Button).Background = Vis != Visibility.Visible ? Brushes.DodgerBlue : Brushes.WhiteSmoke;
            }
        }

        private void Fluxgate_Click(object sender, RoutedEventArgs e)
        {
            Fluxgate FluxgateWindow = new Fluxgate();
            FluxgateWindow.Show();

        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
