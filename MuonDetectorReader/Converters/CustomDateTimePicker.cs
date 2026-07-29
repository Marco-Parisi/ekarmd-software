using MahApps.Metro.Controls;
using System;
using System.Globalization;
using System.Windows;

namespace MuonDetectorReader.Converters
{
    /// <summary>
    /// Custom DateTimePicker that overrides the text display format
    /// to show dates in "dd/MM/yy HH:mm" format.
    /// </summary>
    public class CustomDateTimePicker : DateTimePicker
    {
        static CustomDateTimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CustomDateTimePicker),
                new FrameworkPropertyMetadata(typeof(DateTimePicker)));
        }

        public CustomDateTimePicker()
        {
            // Set Italian culture for dd/MM/yy format base
            var customCulture = (CultureInfo)CultureInfo.GetCultureInfo("it-IT").Clone();
            customCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yy";
            customCulture.DateTimeFormat.ShortTimePattern = "HH:mm";
            Culture = customCulture;

            ControlsHelper.SetCornerRadius(this, (CornerRadius)App.Current.FindResource("GlobalCornerRadius"));
        }
    }
}
