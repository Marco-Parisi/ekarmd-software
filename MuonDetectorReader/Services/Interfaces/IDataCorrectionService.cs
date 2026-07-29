using System;
using System.Collections.Generic;
using MuonDetectorReader.Models;

namespace MuonDetectorReader.Services.Interfaces
{
    public interface IDataCorrectionService
    {
        CorrectionResult GenerateCorrectedCounts(DetectorDataSet data, double beta, double sigmaBeta, double referencePressure, double kt, double sigmaKt, bool removeOutliers, double outlierSigma);
        List<string> CheckDataGaps(List<DateTime> dates);

        List<DateTime> GetDateGaps();
    }
}
