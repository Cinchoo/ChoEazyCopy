using Cinchoo.Core;
using Cinchoo.Core.Diagnostics;
using Cinchoo.Core.Shell;
using Cinchoo.Core.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ChoEazyCopy
{
    [ChoApplicationHost]
    public class AppHost : ChoApplicationHost
    {
        public const string AppFileExt = ".blk";
        private string _defaultBalloonTipText = null;

        [STAThread]
        public static void Main(string[] args)
        {
            ChoApplication.Run(args);
        }

        protected override void ApplyGlobalApplicationSettingsOverrides(ChoGlobalApplicationSettings obj)
        {
            obj.TrayApplicationBehaviourSettings.TurnOn = true;
            obj.TrayApplicationBehaviourSettings.HideTrayIconWhenMainWindowShown = true;
            obj.TrayApplicationBehaviourSettings.ContextMenuSettings.DisplayHelpMenuItem = false;
            obj.TrayApplicationBehaviourSettings.ContextMenuSettings.DisplayAboutMenuItem = false;
            obj.TrayApplicationBehaviourSettings.ContextMenuSettings.DisplayShowMainWndMenuItem = true;
            obj.TrayApplicationBehaviourSettings.ContextMenuSettings.DisplayShowInTaskbarMenuItem = false;

            obj.ApplicationBehaviourSettings.SingleInstanceApp = false;
            obj.ApplicationBehaviourSettings.ActivateFirstInstance = true;
        }

        protected override void OnWindowMinimize(ChoNotifyIcon notifyIcon)
        {
            ChoApplication.NotifyIcon.ShowBalloonTip(_defaultBalloonTipText, 500);
        }

        protected override void OnStart(string[] args)
        {
            _defaultBalloonTipText = "{0} is running...".FormatString(ChoGlobalApplicationSettings.Me.ApplicationName);

            try
            {
                ChoShellExtension.Register();
                ChoTrace.WriteLine("Shell Extensions registered successfully.");
            }
            catch (Exception ex)
            {
                ChoTrace.WriteLine("Failed to register Shell Extensions. {0}".FormatString(ex.Message));
            }
            try
            {
                ChoShellFileAssociation.Register();
                ChoTrace.WriteLine("File Associations registered successfully.");
            }
            catch (Exception ex)
            {
                ChoTrace.WriteLine("Failed to register File Associations. {0}".FormatString(ex.Message));
            }

            if (ChoApplication.ApplicationMode == ChoApplicationMode.Console)
            {
                ChoAppCmdLineArgs cmdLineArgs = new ChoAppCmdLineArgs();
                cmdLineArgs.StartFileCopy();
            }
            base.OnStart(args);
        }

        public override object MainWindowObject
        {
            get
            {
                ChoFileAssociationCmdLineArgs cmd = new ChoFileAssociationCmdLineArgs();
                if (cmd.IsAppFile)
                    return new MainWindow();//cmd.SettingsFilePath);
                else
                    return new MainWindow();
            }
        }

        protected override void AfterNotifyIconConstructed(ChoNotifyIcon ni)
        {
            ni.Text = "Eazy Copy - Cinchoo";
        }
    }

    [ChoShellExtension]
    public class ShellExt
    {
        [ChoShellExtensionContextMenu("Folder", MenuText = "Eazy copy...", DefaultArgPrefix = "/d:")]
        public static void EazyCopyFiles(string[] args)
        {
            ChoApplication.ApplicationMode = ChoApplicationMode.Windows;
            ChoApplication.Run(args);
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }
}
