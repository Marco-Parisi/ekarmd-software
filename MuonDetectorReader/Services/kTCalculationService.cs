using System;
using System.Collections.Generic;
using System.Linq;
using MuonDetectorReader.Models;
using MuonDetectorReader.Services.Interfaces;

namespace MuonDetectorReader.Services
{
    public class kTCalculationService : IkTCalculationService
    {
        private readonly IRegressionService _regressionService;

        public kTCalculationService(IRegressionService regressionService)
        {
            _regressionService = regressionService;
        }

        public KTEstimation EstimateKT(List<double> temperature, List<double> pressureCorrectedCounts)
        {
            if (temperature.Count == 0 || pressureCorrectedCounts.Count == 0 || temperature.Count != pressureCorrectedCounts.Count)
                return KTEstimation.Empty;

            double tAvg = temperature.Average();

            List<double> tmT0 = new List<double>();
            List<double> logCount = new List<double>();
            List<double> errlogCount = new List<double>();

            for (int i = 0; i < temperature.Count; i++)
            {
                tmT0.Add(temperature[i] - tAvg);
                logCount.Add(Math.Log(pressureCorrectedCounts[i]));
                errlogCount.Add(1 / Math.Sqrt(pressureCorrectedCounts[i]));
            }

            var (m, sigma_m, _) = _regressionService.LinearRegression(tmT0, logCount, errlogCount);

            return new KTEstimation
            {
                KT = m,
                SigmaKT = sigma_m,
                AverageTemperature = tAvg
            };
        }
    }
}
