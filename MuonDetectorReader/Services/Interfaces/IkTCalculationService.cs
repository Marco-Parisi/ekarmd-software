using System.Collections.Generic;
using MuonDetectorReader.Models;

namespace MuonDetectorReader.Services.Interfaces
{
    public interface IkTCalculationService
    {
        KTEstimation EstimateKT(List<double> temperature, List<double> pressureCorrectedCounts);
    }
}
