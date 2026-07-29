using System;
using System.Collections.Generic;
using MuonDetectorReader.Models;
using MuonDetectorReader.Services.Interfaces;

namespace MuonDetectorReader.Services
{
    public class BetaCalculationService : IBetaCalculationService
    {
        private readonly IRegressionService _regressionService;

        public BetaCalculationService(IRegressionService regressionService)
        {
            _regressionService = regressionService;
        }

        public BetaEstimation EstimateBeta(List<double> pressure, List<double> rawCounts, double referencePressure)
        {
            if (pressure.Count == 0 || rawCounts.Count == 0 || pressure.Count != rawCounts.Count)
                return BetaEstimation.Empty;

            List<double> pmP0 = new List<double>();
            List<double> logCount = new List<double>();
            List<double> errlogCount = new List<double>();

            for (int i = 0; i < pressure.Count; i++)
            {
                pmP0.Add(pressure[i] - referencePressure);
                logCount.Add(Math.Log(rawCounts[i]));
                errlogCount.Add(1 / Math.Sqrt(rawCounts[i]));
            }

            var (m, sigma_m, _) = _regressionService.LinearRegression(pmP0, logCount, errlogCount);

            return new BetaEstimation
            {
                Beta = m,
                SigmaBeta = sigma_m,
                ReferencePressure = referencePressure
            };
        }
    }
}
