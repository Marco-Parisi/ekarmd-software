using MuonDetectorReader.Models;

namespace MuonDetectorReader.Services.Interfaces
{
    public interface ISettingsService
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
