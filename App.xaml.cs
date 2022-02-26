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
using System.IO;
using System.Diagnostics;
using ChoEazyCopy.Properties;
using System.Reflection;
using System.Security.Principal;
using MahApps.Metro;

namespace ChoEazyCopy
{
    [ChoApplicationHost]
    public class AppHost : ChoApplicationHost
    {
        public const string AppFileExt = ".ezy";
        private string _defaultBalloonTipText = null;

        [STAThread]
        public static void Main(string[] args)
        {
            ChoApplication.Run(args);
        }
        public override void OnTrayAppExitMenuClicked(object sender, EventArgs e)
        {
            ChoApplication.NotifyIcon.Dispose();
            base.OnTrayAppExitMenuClicked(sender, e);
        }

        protected override void ApplyGlobalApplicationSettingsOverrides(ChoGlobalApplicationSettings obj)
        {
            obj.TrayApplicationBehaviourSettings.TurnOn = true;
            obj.TrayApplicationBehaviourSettings.TurnOnMode = ChoTrayAppTurnOnMode.OnMinimize;
            obj.TrayApplicationBehaviourSettings.HideTrayIconWhenMainWindowShown = false;
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

            UnregisterShellExtensions();

            if (ChoApplication.ApplicationMode == ChoApplicationMode.Console)
            {
                ChoAppCmdLineArgs cmdLineArgs = new ChoAppCmdLineArgs();
                cmdLineArgs.StartFileCopy();
            }
            
            base.OnStart(args);
        }

        public static void RegisterShellExtensions()
        {
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
        }

        public static void UnregisterShellExtensions()
        {
            try
            {
                ChoShellExtension.Unregister();
                ChoTrace.WriteLine("Shell Extensions unregistered successfully.");
            }
            catch (Exception ex)
            {
                ChoTrace.WriteLine("Failed to unregister Shell Extensions. {0}".FormatString(ex.Message));
            }
            try
            {
                ChoShellFileAssociation.Unregister();
                ChoTrace.WriteLine("File Associations unregistered successfully.");
            }
            catch (Exception ex)
            {
                ChoTrace.WriteLine("Failed to unregister File Associations. {0}".FormatString(ex.Message));
            }
        }

        public static void RunAsAdmin()
        {
            if (!IsRunAsAdmin())
            {
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = Assembly.GetEntryAssembly().CodeBase;

                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    proc.Arguments += String.Format("\"{0}\" ", arg);
                }

                proc.Verb = "runas";

                try
                {
                    Process.Start(proc);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    ChoTrace.WriteLine("This application requires elevated credentials in order to operate correctly! {0}".FormatString(ex.Message));
                }
            }
        }

        internal static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public override object MainWindowObject
        {
            get
            {
                ChoFileAssociationCmdLineArgs cmd = new ChoFileAssociationCmdLineArgs();
                if (cmd.IsAppFile)
                    return new MainWindow(cmd.SettingsFilePath);
                else
                    return new MainWindow();
            }
        }

        public override object ApplicationObject
        {
            get
            {
                if (Application.Current == null)
                    new App();
                ////ThemeManager.ChangeAppStyle(Application.Current,
                ////                            ThemeManager.GetAccent("Blue"),
                ////                            ThemeManager.GetAppTheme("BaseDark"));
                //ThemeManager.ChangeAppStyle(Application.Current,
                //                            ThemeManager.GetAccent("Blue"),
                //                            ThemeManager.GetAppTheme("BaseDark"));

                Application.Current.Exit += (o, e) => ChoApplication.NotifyIcon.Dispose();
                return Application.Current;
            }
        }

        protected override void AfterNotifyIconConstructed(ChoNotifyIcon ni)
        {
            ni.Text = "ChoEazyCopy - Cinchoo";

            ni.ContextMenuStrip.Items.Insert(1, new System.Windows.Forms.ToolStripMenuItem("Launch New Instance",
                System.Drawing.Image.FromStream(this.GetType().Assembly.GetManifestResourceStream("ChoEazyCopy.Resources.OpenNewWindow.png")),
                ((o, e) =>
                {
                    var info = new System.Diagnostics.ProcessStartInfo(ChoApplication.EntryAssemblyLocation);
                    System.Diagnostics.Process.Start(info);
                })));
            if (!IsRunAsAdmin())
            {
                ni.ContextMenuStrip.Items.Insert(2, new System.Windows.Forms.ToolStripMenuItem("Run as Administrator",
                  System.Drawing.Image.FromStream(this.GetType().Assembly.GetManifestResourceStream("ChoEazyCopy.Resources.Security.png")),
                    ((o, e) =>
                    {
                        AppHost.RunAsAdmin();
                    })));
            }
            else
            {
                ni.ContextMenuStrip.Items.Insert(2, new System.Windows.Forms.ToolStripMenuItem("Register Shell Extensions",
                  System.Drawing.Image.FromStream(this.GetType().Assembly.GetManifestResourceStream("ChoEazyCopy.Resources.Registry.png")),
                  ((o, e) =>
                  {
                      AppHost.RegisterShellExtensions();
                  })));
                ni.ContextMenuStrip.Items.Insert(3, new System.Windows.Forms.ToolStripMenuItem("Unregister Shell Extensions",
                  System.Drawing.Image.FromStream(this.GetType().Assembly.GetManifestResourceStream("ChoEazyCopy.Resources.RemoveRegistry.png")),
                   ((o, e) =>
                   {
                       AppHost.UnregisterShellExtensions();
                   })));

            }
        }

        public void ShowMainWindow()
        {
            var a = ApplicationObject;
            var wnd = MainWindowObject;
            if (wnd is Window)
            {
                ChoWindowsManager.HideConsoleWindow();
                ((Window)wnd).ShowDialog();
            }
        }
    }

    [ChoShellExtension]
    public class ShellExt
    {
        [ChoShellExtensionContextMenu("Folder", MenuText = "Open with ChoEazyCopy...", DefaultArgPrefix = "/d:")]
        public static void EazyCopyFiles(string[] args)
        {
            new AppHost().ShowMainWindow();
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }
    }
}
