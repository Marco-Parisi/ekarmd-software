using System.Threading.Tasks;

namespace MuonDetectorReader.Services.Interfaces
{
    public interface IMessageBoxService
    {
        void Show(string message, string title);
        Task ShowAsync(string message, string title);
        Task<bool> ShowConfirmAsync(string message, string title, string yesText = null, string noText = null);
    }
}
