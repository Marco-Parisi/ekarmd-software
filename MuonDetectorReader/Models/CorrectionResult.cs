using System.Collections.Generic;

namespace MuonDetectorReader.Models
{
    public class CorrectionResult
    {
        public List<double> PressureCorrectedCounts { get; set; }
        public List<double> FullCorrectedCounts { get; set; }
        public List<double> DeltaPressureCorrected { get; set; }
        public List<double> DeltaFullCorrected { get; set; }
        public double TemperatureAverage { get; set; }
    }
}
