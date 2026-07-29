using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MuonDetectorReader.Models;
using MuonDetectorReader.Services.Interfaces;

namespace MuonDetectorReader.Services
{
    public class FileParserService : IFileParserService
    {
        private static readonly Regex charRegex = new Regex("[a-z]+");

        public DetectorDataSet Parse(string filePath)
        {
            var data = new DetectorDataSet();
            
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            data.SourceFileName = Path.GetFileName(filePath);

            bool csv = filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

            using (StreamReader sr = new StreamReader(filePath))
            {
                while (!sr.EndOfStream)
                {
                    string str = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(str)) continue;

                    if (csv)
                    {
                        if (str.Contains(";"))
                        {
                            str = str.Replace(";", "*");
                            str = str.Replace(",", ".");
                        }
                        else if (str.Contains(","))
                        {
                            str = str.Replace(",", "*");
                        }
                    }
                    else
                    {
                        str = str.Replace(",", ".");
                    }

                    if (charRegex.IsMatch(str))
                        str = charRegex.Replace(str, "");

                    if (str.Contains("*") && (str.Contains("/") || str.Contains("-")) && !charRegex.IsMatch(str))
                    {
                        if (str.Contains("-"))
                            str = str.Replace("-", "/");

                        if (str.Contains("   "))
                            str = str.Replace("   ", "");

                        string dateStr = str.Remove(str.IndexOf("*") - 1);
                        data.Dates.Add(Convert.ToDateTime(dateStr));

                        str = str.Remove(0, str.IndexOf("*") + 1);
                        string tempStr = str.Remove(str.IndexOf("*")).Replace(".", ",");
                        data.Temperature.Add(Convert.ToDouble(tempStr));

                        str = str.Remove(0, str.IndexOf("*") + 1);
                        string pressStr = str.Remove(str.IndexOf("*")).Replace(".", ",");
                        data.Pressure.Add(Convert.ToDouble(pressStr));

                        str = str.Remove(0, str.IndexOf("*") + 1);
                        if (str.Contains(".") || str.Contains(","))
                            data.RawCounts.Add((int)Convert.ToDouble(str.Replace(".", ",")));
                        else
                            data.RawCounts.Add(Convert.ToInt32(str));
                    }
                }
            }

            if (data.Dates.Count == 0 || data.Temperature.Count == 0 || data.Pressure.Count == 0 || data.RawCounts.Count == 0)
                throw new Exception(LocalizationService.Current.GetString("Str_WrongFormat"));

            // Le liste nel file originale venivano invertite dopo il parsing (riga 200)
            data.Dates.Reverse();
            data.Temperature.Reverse();
            data.Pressure.Reverse();
            data.RawCounts.Reverse();

            RemoveDuplicates(data);

            data.OriginalTemperature = new List<double>(data.Temperature);
            data.OriginalPressure = new List<double>(data.Pressure);
            data.OriginalRawCounts = new List<double>(data.RawCounts);

            return data;
        }

        private void RemoveDuplicates(DetectorDataSet data)
        {
            var seen = new HashSet<DateTime>();
            var duplicateIndexes = new List<int>();

            for (int i = 0; i < data.Dates.Count; i++)
            {
                if (!seen.Add(data.Dates[i]))
                {
                    duplicateIndexes.Add(i);
                }
            }

            foreach (var index in duplicateIndexes.OrderByDescending(i => i))
            {
                data.Dates.RemoveAt(index);
                data.Pressure.RemoveAt(index);
                data.Temperature.RemoveAt(index);
                data.RawCounts.RemoveAt(index);
            }
        }

        public string MergeFilesCLI(string cliPath, string detectorName)
        {
            DateTime date = DateTime.Now;
            string year = date.ToString("yyyy");
            string prevYear = date.AddYears(-1).ToString("yyyy");
            
            // Gestione del cambio d'anno: se siamo a inizio anno, carichiamo anche l'anno precedente per avere continuità nei grafici
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(cliPath, "CoolTerm Capture *" + year + "*.txt"));
            
            if (date.Month <= 2) // Aggiungiamo i file dell'anno precedente se siamo in gennaio/febbraio
            {
                files.AddRange(Directory.GetFiles(cliPath, "CoolTerm Capture *" + prevYear + "*.txt"));
            }

            files = files.Distinct().ToList(); // Evita duplicati se per caso il glob prende file simili
            
            // Ordiniamo i file dal più recente al più vecchio come faceva originale (Reverse() dopo GetFiles)
            files.Sort(); 
            files.Reverse();

            string mergeDir = Path.Combine(cliPath, "merge");
            if (!Directory.Exists(mergeDir))
                Directory.CreateDirectory(mergeDir);

            string outfilename = Path.Combine(mergeDir, $"{detectorName} {year}.txt");

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
                List<string> headers = outLines.Where(l => l.Length > 4 && !char.IsDigit(l[4])).ToList();

                // 2. separazione righe che iniziano con l'anno o comunque dati
                List<string> dataRows = outLines.Where(l => l.Length > 4 && char.IsDigit(l[4])).ToList();

                // 3. ordinamento alfabetico (che per formato YYYY/MM/DD corrisponde a cronologico)
                dataRows.Sort();

                // 4. concatenamento intestazioni e dati ordinati
                outLines = headers.Concat(dataRows).ToList();

                File.WriteAllLines(outfilename, outLines);
            }

            return outfilename;
        }
    }
}
