using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ChoEazyCopy
{
    public class ChoAppTheme : INotifyPropertyChanged
    {
        public static readonly ChoAppTheme Instance = new ChoAppTheme();

        private Accent _accent = ThemeManager.GetAccent("Blue");
        private AppTheme _appTheme = ThemeManager.GetAppTheme("BaseDark");
        private bool _isDarkMode = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Refresh(Accent accent, AppTheme appTheme, bool isDarkMode)
        {
            _accent = accent;
            _appTheme = appTheme;
            _isDarkMode = isDarkMode;

            RaisePropertyChanged(nameof(ControlBackgroundBrush));
            RaisePropertyChanged(nameof(ControlForegroundBrush));
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Brush TextBoxFocusBorderBrush
        {
            get
            {
                return (Brush)_appTheme.Resources["TextBoxFocusBorderBrush"];
            }
        }

        public Brush ControlMouseOverBackgroundBrush
        {
            get
            {
                return (Brush)_appTheme.Resources["GrayHoverBrush"];
            }
        }

        public Brush ControlBackgroundBrush
        {
            get
            {
                if (_isDarkMode)
                    return (Brush)_appTheme.Resources["BlackBrush"];
                else
                    return (Brush)_appTheme.Resources["WhiteBrush"];
            }
        }

        public Brush ControlForegroundBrush
        {
            get
            {
                if (_isDarkMode)
                    return new SolidColorBrush(Colors.White);
                else
                    return new SolidColorBrush(Colors.Black);
                if (!_isDarkMode)
                    return (Brush)_appTheme.Resources["BlackBrush"];
                else
                    return (Brush)_appTheme.Resources["WhiteBrush"];
            }
        }

        public Brush PGControlBackgroundBrush
        {
            get
            {
                //if (_isDarkMode)
                //    return (Brush)_appTheme.Resources["WhiteBrush"];
                //else
                return System.Windows.SystemColors.ControlLightBrush;
            }
        }

        public Brush PGControlForegroundBrush
        {
            get
            {
                if (_isDarkMode)
                    return (Brush)_appTheme.Resources["WhiteBrush"];
                else
                    return (Brush)_appTheme.Resources["BlackBrush"];
            }
        }

        public Brush PGControlBorderBrush
        {
            get
            {
                return (Brush)_appTheme.Resources["ControlBorderBrush"];
            }
        }

        public Brush ControlBorderBrush
        {
            get
            {
                return (Brush)_appTheme.Resources["ControlBorderBrush"];
            }
        }

        public Brush WindowTitleColorBrush
        {
            get
            {
                return (Brush)_appTheme.Resources["WindowTitleColorBrush"];
            }
        }

        public Brush ThemeForegroundBrush
        {
            get
            {
                return (Brush)_appTheme.Resources["BlackBrush"];
            }
        }

        public Brush ThemeBackgroundBrush
        {
            get
            {
                return (Brush)_accent.Resources["AccentColorBrush"];
            }
        }

        public Brush WindowTitleBrush
        {
            get
            {
                return (Brush)_appTheme.Resources["WindowTitleColorBrush"];
            }
        }
        public Brush MouseOverBrush
        {
            get
            {
                return (Brush)_appTheme.Resources["ButtonMouseOverBorderBrush"];
            }
        }

        public Brush TabControlBackgroundBrush
        {
            get
            {
                return (Brush)_accent.Resources["AccentColorBrush"];
            }
        }

        public Brush TabControlForegroundBrush
        {
            get
            {
                //if (!_isDarkMode)
                //    return (Brush)_appTheme.Resources["BlackBrush"];
                //else
                    return (Brush)_appTheme.Resources["WhiteBrush"];
            }
        }
    }
}
