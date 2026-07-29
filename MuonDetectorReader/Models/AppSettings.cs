namespace MuonDetectorReader.Models
{
    public class AppSettings
    {
        public double ReferencePressure { get; set; } = 1013.25;
        public double Beta { get; set; } = 0;
        public double SigmaBeta { get; set; } = 0;
        public double KT { get; set; } = -0.00170065228125;
        public double SigmaKT { get; set; } = 1.3346453895E-18;
        public bool IsSidebarOnRight { get; set; } = false;
        public bool IsSeriesDottedChecked { get; set; } = false;
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = null;
    }
}
