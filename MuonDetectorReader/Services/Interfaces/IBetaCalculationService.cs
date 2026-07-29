using System.Collections.Generic;
using MuonDetectorReader.Models;

namespace MuonDetectorReader.Services.Interfaces
{
    public interface IBetaCalculationService
    {
        BetaEstimation EstimateBeta(List<double> pressure, List<double> rawCounts, double referencePressure);
    }
}
