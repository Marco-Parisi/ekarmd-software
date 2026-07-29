using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MuonDetectorReader.Services.Interfaces;
using MuonDetectorReader.Utils;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MuonDetectorReader.Services
{
    public class MessageBoxService : IMessageBoxService
    {
        private async Task ShowMetroMessageAsync(string title, string message)
        {
            var window = Application.Current.Windows.OfType<MetroWindow>().FirstOrDefault(x => x.IsActive) 
                         ?? Application.Current.MainWindow as MetroWindow;
            if (window != null)
            {
                BlurHelper.BlurShow();

                var settings = new MetroDialogSettings
                {
                    AffirmativeButtonText = LocalizationService.Current.GetString("Str_OK") ?? "OK",
                    AnimateShow = false,
                    AnimateHide = false,
                    ColorScheme = MetroDialogColorScheme.Accented
                };

                await window.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, settings); 
                
                BlurHelper.BlurHide();
            }
            else
            {
                MessageBox.Show(message, title);
            }
        }

        private async Task<bool> ShowMetroConfirmAsync(string title, string message, string yesText, string noText)
        {
            var window = Application.Current.Windows.OfType<MetroWindow>().FirstOrDefault(x => x.IsActive) 
                         ?? Application.Current.MainWindow as MetroWindow;
            if (window != null)
            {
                BlurHelper.BlurShow();

                var settings = new MetroDialogSettings
                {
                    AffirmativeButtonText = !string.IsNullOrEmpty(yesText) ? yesText : (LocalizationService.Current.GetString("Str_Yes") ?? "Yes"),
                    NegativeButtonText = !string.IsNullOrEmpty(noText) ? noText : (LocalizationService.Current.GetString("Str_No") ?? "No"),
                    AnimateShow = false,
                    AnimateHide = false,
                    ColorScheme = MetroDialogColorScheme.Accented
                };
                var result = await window.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, settings);

                BlurHelper.BlurHide();

                return result == MessageDialogResult.Affirmative;
            }
            else
            {
                return MessageBox.Show(message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            }
        }

        public void Show(string message, string title)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                _ = ShowMetroMessageAsync(title, message);
            }
            else
            {
                Application.Current.Dispatcher.InvokeAsync(() => ShowMetroMessageAsync(title, message));
            }
        }

        public async Task ShowAsync(string message, string title)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                await ShowMetroMessageAsync(title, message);
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(() => ShowMetroMessageAsync(title, message)).Task.Unwrap();
            }
        }

        public async Task<bool> ShowConfirmAsync(string message, string title, string yesText = null, string noText = null)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                return await ShowMetroConfirmAsync(title, message, yesText, noText);
            }
            else
            {
                return await Application.Current.Dispatcher.InvokeAsync(() => ShowMetroConfirmAsync(title, message, yesText, noText)).Task.Unwrap();
            }
        }
    }
}
