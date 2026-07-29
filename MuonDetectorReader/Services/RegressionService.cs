using System;
using System.Collections.Generic;
using MuonDetectorReader.Services.Interfaces;

namespace MuonDetectorReader.Services
{
    public class RegressionService : IRegressionService
    {
        public (double slope, double sigmaSlope, double intercept) LinearRegression(List<double> xList, List<double> yList, List<double> yErrList = null)
        {
            int numPoints = yList.Count;

            double sumW = 0;
            double sumWX = 0;
            double sumWY = 0;
            double sumWXX = 0;
            double sumWXY = 0;

            bool hasWeights = yErrList != null && yErrList.Count == numPoints;

            for (int i = 0; i < numPoints; i++)
            {
                double x = xList[i];
                double y = yList[i];

                double w = 1.0;
                if (hasWeights)
                {
                    double sigma = yErrList[i];
                    w = 1.0 / (sigma * sigma);
                }

                sumW += w;
                sumWX += w * x;
                sumWY += w * y;
                sumWXX += w * x * x;
                sumWXY += w * x * y;
            }

            double delta = (sumW * sumWXX) - (sumWX * sumWX);

            double m = ((sumW * sumWXY) - (sumWX * sumWY)) / delta;
            double q = ((sumWXX * sumWY) - (sumWX * sumWXY)) / delta;

            double sigma_m = Math.Sqrt(sumW / delta);

            return (m, sigma_m, q);
        }
    }
}
