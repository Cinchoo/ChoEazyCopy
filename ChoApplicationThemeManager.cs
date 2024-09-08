using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChoEazyCopy
{
    public static class ChoApplicationThemeManager
    {
        static ChoApplicationThemeManager()
        {
            _theme = "BaseLight";
            _accent = "Steel";
        }
        private static string _theme;
        public static string Theme
        {
            get { return _theme; }
            set
            {
                _theme = value;
                ApplyTheme();
            }
        }
        private static string _accent;
        public static string Accent
        {
            get { return _accent; }
            set
            {
                _accent = value;
                ApplyTheme();
            }
        }

        public static void ApplyTheme()
        {
            MainWindow wnd = Application.Current.MainWindow as MainWindow;

            ThemeManager.ChangeAppStyle(wnd,
                            ThemeManager.GetAccent(Accent),
                            ThemeManager.GetAppTheme(Theme));
            wnd.RefreshWindow();
        }
    }
}
