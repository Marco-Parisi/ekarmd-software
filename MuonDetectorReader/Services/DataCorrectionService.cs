using System;
using System.Collections.Generic;
using System.Linq;
using MuonDetectorReader.Models;
using MuonDetectorReader.Services.Interfaces;
using MuonDetectorReader.Utils;

namespace MuonDetectorReader.Services
{
    public class DataCorrectionService : IDataCorrectionService
    {
        private List<DateTime> DateGaps = new List<DateTime>();
        public CorrectionResult GenerateCorrectedCounts(DetectorDataSet data, double beta, double sigmaBeta, double referencePressure, double kt, double sigmaKt, bool removeOutliers, double outlierSigma)
        {
            var result = new CorrectionResult
            {
                PressureCorrectedCounts = new List<double>(),
                FullCorrectedCounts = new List<double>(),
                DeltaPressureCorrected = new List<double>(),
                DeltaFullCorrected = new List<double>()
            };

            if (data.RawCounts.Count == 0)
                return result;

            if (removeOutliers)
            {
                data.Temperature = OutlierRemover.RemoveOutliersSigma(data.OriginalTemperature, outlierSigma);
                data.Pressure = OutlierRemover.RemoveOutliersSigma(data.OriginalPressure, outlierSigma);
                data.RawCounts = OutlierRemover.RemoveOutliersSigma(data.OriginalRawCounts, outlierSigma);
            }
            else
            {
                data.Temperature = new List<double>(data.OriginalTemperature);
                data.Pressure = new List<double>(data.OriginalPressure);
                data.RawCounts = new List<double>(data.OriginalRawCounts);
            }

            data.PressureMinusRef.Clear();
            foreach (var p in data.Pressure)
            {
                data.PressureMinusRef.Add(p - referencePressure);
            }

            for (int i = 0; i < data.PressureMinusRef.Count; i++)
            {
                result.PressureCorrectedCounts.Add(Math.Exp(-beta * data.PressureMinusRef[i]) * data.RawCounts[i]);
            }

            result.DeltaPressureCorrected = PressureErrorPropagation(data.RawCounts, result.PressureCorrectedCounts, data.Pressure, beta, sigmaBeta, referencePressure);

            result.TemperatureAverage = data.Temperature.Average();
            List<double> TmT0 = new List<double>();
            foreach (var t in data.Temperature)
            {
                TmT0.Add(t - result.TemperatureAverage);
            }

            for (int i = 0; i < result.PressureCorrectedCounts.Count; i++)
            {
                result.FullCorrectedCounts.Add(result.PressureCorrectedCounts[i] * Math.Exp(-kt * TmT0[i]));
            }

            result.DeltaFullCorrected = TemperatureErrorPropagation(result.PressureCorrectedCounts, result.DeltaPressureCorrected, result.FullCorrectedCounts, data.Temperature, result.TemperatureAverage, kt, sigmaKt);

            // Populate the data model
            data.PressureCorrectedCounts = result.PressureCorrectedCounts;
            data.FullCorrectedCounts = result.FullCorrectedCounts;
            data.DeltaPressureCorrected = result.DeltaPressureCorrected;
            data.DeltaFullCorrected = result.DeltaFullCorrected;

            return result;
        }

        private List<double> PressureErrorPropagation(List<double> rawCounts, List<double> corrCounts, List<double> pressure, double beta, double sigmaBeta, double refPress)
        {
            double DeltaP = 0.5; // hPa
            List<double> results = new List<double>();

            for (int i = 0; i < corrCounts.Count; i++)
            {
                double N_raw = rawCounts[i];
                double N = corrCounts[i];
                double P = pressure[i];

                double term1 = 1.0 / N_raw;
                double term2 = Math.Pow(beta * DeltaP, 2);
                double term3 = Math.Pow((P - refPress) * sigmaBeta, 2);
                double sumUnderRoot = term1 + term2 + term3;

                double deltaN = N * Math.Sqrt(sumUnderRoot);
                results.Add(deltaN);
            }

            return results;
        }

        private List<double> TemperatureErrorPropagation(List<double> corrCounts, List<double> deltaCorrCounts, List<double> fullCorrCounts, List<double> temperature, double tempAvg, double kt, double sigmaKt)
        {
            double DeltaT = 0.5; // °C
            List<double> results = new List<double>();

            for (int i = 0; i < fullCorrCounts.Count; i++)
            {
                double N_p = corrCounts[i];
                double deltaN_p = deltaCorrCounts[i];
                double N_full = fullCorrCounts[i];
                double T = temperature[i];

                double term1 = Math.Pow(deltaN_p / N_p, 2);
                double term2 = Math.Pow(kt * DeltaT, 2);
                double term3 = Math.Pow((T - tempAvg) * sigmaKt, 2);
                double sumUnderRoot = term1 + term2 + term3;

                double deltaN = N_full * Math.Sqrt(sumUnderRoot);
                results.Add(deltaN);
            }

            return results;
        }

        public List<string> CheckDataGaps(List<DateTime> dates)
        {
            List<string> strgaps = new List<string>();
            double integrationTime = 1.0; // hours

            for (int i = 0; i < dates.Count - 1; i++)
            {
                TimeSpan diff = dates[i] - dates[i + 1];
                if (diff.TotalHours > integrationTime)
                {
                    DateGaps.Add(dates[i]);
                    string hoursStr = LocalizationService.Current.GetString("Str_Hours");
                    string alert = $"{dates[i + 1].ToString("dd/MM/yy HH:mm")} - {dates[i].ToString("dd/MM/yy HH:mm")} → {diff.TotalHours} {hoursStr}\n";
                    strgaps.Add(alert);
                }
            }

            return strgaps;
        }

        public List<DateTime> GetDateGaps() 
        {
            return DateGaps;
        }
    }
}
