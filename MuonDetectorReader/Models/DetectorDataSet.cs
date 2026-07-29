using System;
using System.Collections.Generic;

namespace MuonDetectorReader.Models
{
    public class DetectorDataSet
    {
        public List<DateTime> Dates { get; set; }
        public List<double> Temperature { get; set; }
        public List<double> Pressure { get; set; }
        public List<double> RawCounts { get; set; }
        public List<double> OriginalTemperature { get; set; }
        public List<double> OriginalPressure { get; set; }
        public List<double> OriginalRawCounts { get; set; }
        public List<double> PressureCorrectedCounts { get; set; }
        public List<double> FullCorrectedCounts { get; set; }
        public List<double> DeltaPressureCorrected { get; set; }
        public List<double> DeltaFullCorrected { get; set; }
        public List<double> PressureMinusRef { get; set; }
        public string SourceFileName { get; set; }

        public DetectorDataSet()
        {
            Dates = new List<DateTime>();
            Temperature = new List<double>();
            Pressure = new List<double>();
            RawCounts = new List<double>();
            OriginalTemperature = new List<double>();
            OriginalPressure = new List<double>();
            OriginalRawCounts = new List<double>();
            PressureCorrectedCounts = new List<double>();
            FullCorrectedCounts = new List<double>();
            DeltaPressureCorrected = new List<double>();
            DeltaFullCorrected = new List<double>();
            PressureMinusRef = new List<double>();
        }

        public void Clear()
        {
            Dates.Clear();
            Temperature.Clear();
            Pressure.Clear();
            RawCounts.Clear();
            OriginalTemperature.Clear();
            OriginalPressure.Clear();
            OriginalRawCounts.Clear();
            PressureCorrectedCounts.Clear();
            FullCorrectedCounts.Clear();
            DeltaPressureCorrected.Clear();
            DeltaFullCorrected.Clear();
            PressureMinusRef.Clear();
        }
    }
}
