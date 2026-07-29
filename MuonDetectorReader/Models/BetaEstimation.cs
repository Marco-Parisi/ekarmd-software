namespace MuonDetectorReader.Models
{
    public class BetaEstimation
    {
        public double Beta { get; set; }
        public double SigmaBeta { get; set; }
        public double ReferencePressure { get; set; }

        public static BetaEstimation Empty
        {
            get
            {
                return new BetaEstimation
                {
                    Beta = 0,
                    SigmaBeta = 0,
                    ReferencePressure = 1013.25
                };
            }
        }
    }
}
