using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ChoEazyCopy
{
    public static class ChoAppTheme
    {
        public static Brush TextBoxFocusBorderBrush
        {
            get
            {
                return (Brush)MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current.MainWindow).Item1.Resources["TextBoxFocusBorderBrush"];
            }
        }

        public static Brush ControlMouseOverBackgroundBrush
        {
            get
            {
                return (Brush)MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current.MainWindow).Item1.Resources["GrayHoverBrush"];
            }
        }

        public static Brush ControlBackgroundBrush
        {
            get
            {
                return (Brush)MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current.MainWindow).Item1.Resources["ControlBackgroundBrush"];
            }
        }

        public static Brush ControlForegroundBrush
        {
            get
            {
                return (Brush)MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current.MainWindow).Item1.Resources["TextBrush"];
            }
        }

        public static Brush ControlBorderBrush
        {
            get
            {
                return (Brush)MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current.MainWindow).Item1.Resources["ControlBorderBrush"];
            }
        }

        public static Brush WindowTitleColorBrush
        {
            get
            {
                return (Brush)MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current.MainWindow).Item1.Resources["WindowTitleColorBrush"];
            }
        }
    }
}
