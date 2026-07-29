using MuonDetectorReader.Models;

namespace MuonDetectorReader.Services.Interfaces
{
    public interface IFileParserService
    {
        DetectorDataSet Parse(string filePath);
        string MergeFilesCLI(string cliPath, string detectorName);
    }
}
