namespace MuonDetectorReader.Models
{
    public class KTEstimation
    {
        public double KT { get; set; }
        public double SigmaKT { get; set; }
        public double AverageTemperature { get; set; }

        public static KTEstimation Empty
        {
            get
            {
                return new KTEstimation
                {
                    KT = 0,
                    SigmaKT = 0,
                    AverageTemperature = 0
                };
            }
        }
    }
}
