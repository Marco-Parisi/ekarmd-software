using System.Windows;
using System.Windows.Media.Effects;

namespace MuonDetectorReader.Utils
{
    public static class BlurHelper
    {
        public static void BlurShow(double radius=12)
        {
            if (Application.Current.MainWindow == null)
                return;

            BlurEffect blurEffect = new BlurEffect { Radius = radius, RenderingBias = RenderingBias.Performance };
            if (Application.Current.MainWindow.Content is System.Windows.UIElement contentElement)
            {
                contentElement.Effect = blurEffect;
            }
        }

        public static void BlurHide()
        {
            if (Application.Current.MainWindow == null)
                return;

            if (Application.Current.MainWindow.Content is System.Windows.UIElement elementToReset)
            {
                elementToReset.Effect = null;
            }
        }
    }
}
