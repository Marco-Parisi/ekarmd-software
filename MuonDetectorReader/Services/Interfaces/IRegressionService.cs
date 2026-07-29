using System.Collections.Generic;

namespace MuonDetectorReader.Services.Interfaces
{
    public interface IRegressionService
    {
        (double slope, double sigmaSlope, double intercept) LinearRegression(List<double> xList, List<double> yList, List<double> yErrList = null);
    }
}
