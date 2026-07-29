using System;
using System.Collections.Generic;
using System.Linq;

namespace MuonDetectorReader.Utils
{
    internal class OutlierRemover
    {
        public static List<double> RemoveOutliersSigma(List<double> data, double sigma = 5.0)
        {
            if (data == null || !data.Any())
                return new List<double>();

            var cleanedData = new List<double>(data);

            double mean = data.Average();
            double sumOfSquares = data.Sum(x => Math.Pow(x - mean, 2));
            double stdDev = Math.Sqrt(sumOfSquares / data.Count);

            double lowerBound = mean - sigma * stdDev;
            double upperBound = mean + sigma * stdDev;

            for (int i = 0; i < cleanedData.Count; i++)
            {
                if (cleanedData[i] < lowerBound || cleanedData[i] > upperBound)
                {
                    cleanedData[i] = mean;
                }
            }

            return cleanedData;
        }

        public static List<double> RemoveOutliersIQR(List<double> data, double k = 1.5)
        {
            if (data == null || !data.Any())
                return new List<double>();

            var cleanedData = new List<double>(data);
            var sortedData = data.OrderBy(x => x).ToList();

            int n = sortedData.Count;
            double q1 = sortedData[(int)Math.Floor(n * 0.25)];
            double q3 = sortedData[(int)Math.Ceiling(n * 0.75) - 1];

            double iqr = q3 - q1;
            double lowerBound = q1 - k * iqr;
            double upperBound = q3 + k * iqr;

            for (int i = 0; i < cleanedData.Count; i++)
            {
                if (cleanedData[i] < lowerBound || cleanedData[i] > upperBound)
                {
                    if (i == 0)
                        cleanedData[i] = cleanedData[i + 1];
                    else if (i == cleanedData.Count - 1)
                        cleanedData[i] = cleanedData[i - 1];
                    else
                        cleanedData[i] = ((cleanedData[i - 1] + cleanedData[i + 1]) / 2.00);
                }
            }
            return cleanedData;
        }
    }
}