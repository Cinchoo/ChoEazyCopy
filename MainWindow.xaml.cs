using Cinchoo.Core;
using Cinchoo.Core.Configuration;
using Cinchoo.Core.Diagnostics;
using Cinchoo.Core.Win32.Dialogs;
using Cinchoo.Core.WPF;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace ChoEazyCopy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        #region Instance Members (Private)

        private readonly string Caption;
        private DispatcherTimer _dispatcherTimer;
        private Thread _mainUIThread;
        private Thread _fileNameProcessThread;
        private ChoAppSettings _appSettings;
        private bool _isRunning = false;
        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                _isRunning = value;
                RaisePropertyChanged(nameof(IsRunning));
            }
        }

        private bool _wndLoaded = false;
        private bool _isNewFileOp = false;
        private bool _isDirty = false;
        private bool IsDirty
        {
            get { return _isDirty; }
            set { _isDirty = value; }
        }
        private string _settingsFilePath = null;
        public string SettingsFilePath
        {
            get { return _settingsFilePath; }
            private set
            {
                _settingsFilePath = value;
                IsDirty = false;
            }
        }
        ChoRoboCopyManager _roboCopyManager = null;
        ChoWPFBindableConfigObject<ChoAppSettings> _bindObj;

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        #endregion Instance Members (Private)

        private bool _scrollOutput = true;
        public bool ScrollOutput
        {
            get { return _scrollOutput; }
            set
            {
                _scrollOutput = value;
                RaisePropertyChanged(nameof(ScrollOutput));
            }
        }

        public MainWindow() :
            this(null)
        {
        }

        public MainWindow(string settingsFilePath)
        {
            SettingsFilePath = settingsFilePath;
            InitializeComponent();

            Caption = Title;
            Title = "{0} (v{1})".FormatString(Title, Assembly.GetEntryAssembly().GetName().Version);
        }

        private void MyWindow_Loaded(object sender1, RoutedEventArgs e1)
        {
            DataContext = this;
            ScrollOutput = false;
            _bindObj = new ChoWPFBindableConfigObject<ChoAppSettings>();
            _appSettings = _bindObj.UnderlyingSource;
            _appSettings.Init();
            if (!SettingsFilePath.IsNullOrWhiteSpace() && File.Exists(SettingsFilePath))
                _appSettings.LoadXml(File.ReadAllText(SettingsFilePath));
            else
                _appSettings.Reset();

            this.DataContext = _appSettings;
            _mainUIThread = Thread.CurrentThread;

            btnNewFile_Click(null, null);

            _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            _dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            _dispatcherTimer.Start();

            string x = _appSettings.SourceDirectory;
            ChoShellExtCmdLineArgs cmdLineArgs = new ChoShellExtCmdLineArgs();
            if (!cmdLineArgs.Directory.IsNullOrWhiteSpace())
                _appSettings.SourceDirectory = cmdLineArgs.Directory;

            IsDirty = false;
        }

        private void RegisterEvents()
        {
            _appSettings.BeforeConfigurationObjectLoaded += _appSettings_BeforeConfigurationObjectLoaded;
            _appSettings.AfterConfigurationObjectMemberSet += _appSettings_AfterConfigurationObjectMemberSet;
            _appSettings.ConfigurationObjectMemberLoadError += _appSettings_ConfigurationObjectMemberLoadError;
            _appSettings.AfterConfigurationObjectPersisted += _appSettings_AfterConfigurationObjectPersisted;
            _appSettings.AfterConfigurationObjectLoaded += _appSettings_AfterConfigurationObjectLoaded;
        }

        private void UnregisterEvents()
        {
            _appSettings.BeforeConfigurationObjectLoaded -= _appSettings_BeforeConfigurationObjectLoaded;
            _appSettings.AfterConfigurationObjectMemberSet -= _appSettings_AfterConfigurationObjectMemberSet;
            _appSettings.ConfigurationObjectMemberLoadError -= _appSettings_ConfigurationObjectMemberLoadError;
            _appSettings.AfterConfigurationObjectPersisted -= _appSettings_AfterConfigurationObjectPersisted;
            _appSettings.AfterConfigurationObjectLoaded -= _appSettings_AfterConfigurationObjectLoaded;
        }

        private void _appSettings_BeforeConfigurationObjectLoaded(object sender, ChoPreviewConfigurationObjectEventArgs e)
        {
            e.Cancel = (bool)this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                 new Func<bool>(() =>
                 {
                     if (IsDirty)
                     {
                         if (MessageBox.Show("Configuration settings has been modified outside of the tool. {0}Do you want to reload it and lose the changes made in the tool?".FormatString(Environment.NewLine),
                             Title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                             return false;
                         else
                             return true;
                     }

                     return false;
                 }));
        }

        private void _appSettings_AfterConfigurationObjectMemberSet(object sender, ChoConfigurationObjectMemberEventArgs e)
        {
            if (_wndLoaded)
            {
                //IsDirty = true;
            }
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                 new Action(() =>
                 {
                     txtRoboCopyCmd.Text = _appSettings.GetCmdLineText();
                 }));
        }

        private void _appSettings_AfterConfigurationObjectLoaded(object sender, ChoConfigurationObjectEventArgs e)
        {
            if (_wndLoaded)
            {
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(() =>
                     {
                         this.DataContext = null;
                         this.DataContext = _appSettings;
                         txtRoboCopyCmd.Text = _appSettings.GetCmdLineText();
                         IsDirty = false;
                     }));
            }
            else
            {
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(() =>
                     {
                         txtRoboCopyCmd.Text = _appSettings.GetCmdLineText();
                         IsDirty = false;
                     }));
            }
        }

        private void MyWindow_ContentRendered(object sender, EventArgs e)
        {
            _wndLoaded = true;
        }

        private void SaveDirectories()
        {
            StringBuilder msg = new StringBuilder();
            //foreach (string folder in lstFolders.Items)
            //{
            //    if (msg.Length == 0)
            //        msg.Append(folder);
            //    else
            //        msg.AppendFormat(";{0}", folder);
            //}

            //_appSettings.Directories = msg.ToString();
        }

        private void _appSettings_AfterConfigurationObjectPersisted(object sender, ChoConfigurationObjectEventArgs e)
        {
            //SaveSettings();
        }

        private void _appSettings_ConfigurationObjectMemberLoadError(object sender, Cinchoo.Core.Configuration.ChoConfigurationObjectMemberErrorEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            grpFolders.IsEnabled = !IsRunning;
            if (IsRunning)
                btnRun.IsEnabled = false;
            else
            {
                if (!txtSourceDirectory.Text.IsNullOrWhiteSpace()
                    && Directory.Exists(txtSourceDirectory.Text)
                    && !txtDestDirectory.Text.IsNullOrWhiteSpace()
                    )
                    btnRun.IsEnabled = true;
                else
                    btnRun.IsEnabled = false;
            }

            btnStop.IsEnabled = IsRunning;
            btnNewFile.IsEnabled = !IsRunning;
            btnOpenFile.IsEnabled = !IsRunning;
            btnSaveFile.IsEnabled = !IsRunning && IsDirty;
            btnSaveAsFile.IsEnabled = !IsRunning;
            if (SettingsFilePath.IsNullOrWhiteSpace())
            {
                this.ToolTip = null;
                tbSettingsName.Text = "<NEW>";
                tbSettingsName.ToolTip = "New File";
            }
            else
            {
                this.ToolTip = SettingsFilePath;
                tbSettingsName.Text = Path.GetFileNameWithoutExtension(SettingsFilePath);
                tbSettingsName.ToolTip = SettingsFilePath;
            }
            btnClear.IsEnabled = !IsRunning && txtStatus.Text.Length > 0;

            if (_fileNameProcessThread != null && _fileNameProcessThread.IsAlive)
            {
            }
            else
            {
            }
            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void btnSourceDirBrowse_Click(object sender, RoutedEventArgs e)
        {
            ChoFolderBrowserDialog dlg1 = new ChoFolderBrowserDialog
            {
                Description = "Choose source folder...",
                ShowNewFolderButton = true,
                ShowEditBox = true,
                ShowBothFilesAndFolders = false,
                NewStyle = true,
                SelectedPath = (System.IO.Directory.Exists(txtSourceDirectory.Text)) ? txtSourceDirectory.Text : "",
                ShowFullPathInEditBox = false,
            };
            dlg1.RootFolder = System.Environment.SpecialFolder.MyComputer;

            var result = dlg1.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(dlg1.SelectedPath))
                    txtSourceDirectory.Text = dlg1.SelectedPath;
                else
                    txtSourceDirectory.Text = System.IO.Path.GetDirectoryName(dlg1.SelectedPath);
            }
        }

        private void btnDestDirBrowse_Click(object sender, RoutedEventArgs e)
        {
            ChoFolderBrowserDialog dlg1 = new ChoFolderBrowserDialog
            {
                Description = "Choose copy/move folder to...",
                ShowNewFolderButton = true,
                ShowEditBox = true,
                ShowBothFilesAndFolders = false,
                NewStyle = true,
                SelectedPath = (System.IO.Directory.Exists(txtDestDirectory.Text)) ? txtDestDirectory.Text : "",
                ShowFullPathInEditBox = false,
            };
            dlg1.RootFolder = System.Environment.SpecialFolder.MyComputer;

            var result = dlg1.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(dlg1.SelectedPath))
                    txtDestDirectory.Text = dlg1.SelectedPath;
                else
                    txtDestDirectory.Text = System.IO.Path.GetDirectoryName(dlg1.SelectedPath);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ChoFramework.Shutdown();
        }

        private void ProcessFiles(object cmd)
        {
            string cmdText = cmd.ToString();
            if (cmdText.IsNullOrWhiteSpace())
                return;

            try
            {
                IsRunning = true;

                _roboCopyManager = new ChoRoboCopyManager();
                _roboCopyManager.Status += (sender, e) => SetStatusMsg(e.Message);
                _roboCopyManager.AppStatus += (sender, e) => UpdateStatus(e.Message, e.Tag.ToNString());

                if (cmdText.IndexOf(' ') >= 0)
                    _roboCopyManager.Process(cmdText.Substring(0, cmdText.IndexOf(' ')), cmdText.Substring(cmdText.IndexOf(' ') + 1));
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                SetStatusMsg(ex.ToString());
            }
            finally
            {
                IsRunning = false;
                _roboCopyManager = null;
            }
        }

        private void SetStatusMsg(string msg)
        {
            if (msg != Environment.NewLine && msg.IsNullOrWhiteSpace()) return;

            if (Thread.CurrentThread != _mainUIThread)
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => SetStatusMsg(msg)));
            }
            else
            {
                Debug.WriteLine(msg);

                //while (txtStatus.Items.Count > _appSettings.MaxStatusMsgSize)
                //{
                //    txtStatus.Items.RemoveAt(0);
                //}

                txtStatus.AppendText(msg);
                if (ScrollOutput)
                    txtStatus.ScrollToEnd();
            }
        }

        private void UpdateStatus(string text, string toolTipText)
        {
            sbAppStatus.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                sbAppStatus.Text = text;
                if (!toolTipText.IsNullOrWhiteSpace())
                    ShowBalloonTipText(toolTipText);
            }));
        }

        private void ShowBalloonTipText(string msg)
        {
            if (ChoApplication.NotifyIcon != null)
            {
                ChoApplication.NotifyIcon.BalloonTipText = msg;
                ChoApplication.NotifyIcon.ShowBalloonTip(500);
            }
        }

        private void tbrMain_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            _fileNameProcessThread = new Thread(new ParameterizedThreadStart(ProcessFiles));
            _fileNameProcessThread.IsBackground = true;
            _fileNameProcessThread.Start(txtRoboCopyCmd.Text);
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(@"Are you sure you want to stop the operation?", Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop, MessageBoxResult.No)
                == MessageBoxResult.No)
                return;

            using (new ChoWPFWaitCursor())
            {
                Thread.Sleep(1000);

                ChoRoboCopyManager roboCopyManager = _roboCopyManager;
                if (roboCopyManager != null)
                    roboCopyManager.Cancel();

                Thread fileNameProcessorThread = _fileNameProcessThread;
                if (fileNameProcessorThread != null)
                {
                    try
                    {
                        fileNameProcessorThread.Abort();
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                    }
                    _fileNameProcessThread = null;
                }
            }
        }

        private void btnSaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(true);
        }

        private void btnSaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(false);
        }

        private bool SaveSettings()
        {
            if (IsDirty)
            {
                string msg;
                if (SettingsFilePath.IsNullOrWhiteSpace())
                    msg = "Do you want to save settings changes to untitled?";
                else
                    msg = "Do you want to save settings changes to '{0}'?".FormatString(Path.GetFileName(SettingsFilePath));
                MessageBoxResult r = MessageBox.Show(msg, Caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (r == MessageBoxResult.Cancel)
                    return true;
                else if (r == MessageBoxResult.No)
                {
                    IsDirty = false;
                    return false;
                }
                else
                    return !SaveSettings(false);
            }
            return false;
        }

        private bool SaveSettings(bool newFile)
        {
            SaveDirectories();

            if (newFile || SettingsFilePath.IsNullOrWhiteSpace())
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.DefaultExt = AppHost.AppFileExt;
                dlg.Filter = "EazyCopy files|*{0}".FormatString(AppHost.AppFileExt);

                var result = dlg.ShowDialog();

                if (result == true)
                    SettingsFilePath = dlg.FileName;
                else
                    return false;
            }

            try
            {
                if (SettingsFilePath.IsNullOrWhiteSpace()) return true;
                File.WriteAllText(SettingsFilePath, _appSettings.ToXml());
                IsDirty = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed saving settings to file. {0}".FormatString(ex.Message), this.Caption, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (SaveSettings())
                return;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = AppHost.AppFileExt;
            dlg.Filter = "EazyCopy files|*{0}".FormatString(AppHost.AppFileExt);

            var result = dlg.ShowDialog();

            if (result == true)
            {
                using (var x = new ChoWPFWaitCursor())
                {
                    _isNewFileOp = true;
                    SettingsFilePath = dlg.FileName;
                    UnregisterEvents();
                    _appSettings.LoadXml(File.ReadAllText(SettingsFilePath));
                    RegisterEvents();
                    this.DataContext = null;
                    this.DataContext = _appSettings;
                    IsDirty = false;
                    _isNewFileOp = false;
                }
            }
        }

        private void btnNewFile_Click(object sender, RoutedEventArgs e)
        {
            if (SaveSettings())
                return;

            using (var x = new ChoWPFWaitCursor())
            {
                _isNewFileOp = true;
                SettingsFilePath = null;
                txtSourceDirectory.Text = String.Empty;
                txtDestDirectory.Text = String.Empty;
                UnregisterEvents();
                _appSettings.Reset();
                RegisterEvents();
                this.DataContext = null;
                this.DataContext = _appSettings;
                IsDirty = false;
                _isNewFileOp = false;
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = String.Empty;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //_wndClosing = true;
            if (IsRunning)
            {
                if (MessageBox.Show("File operation is in progress. Are you sure want to close the application?", Caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    return;
            }

            e.Cancel = SaveSettings();
        }

        private void txtRoboCopyCmd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isNewFileOp)
                IsDirty = true;
        }

        private void btnScrollOutput_Click(object sender, RoutedEventArgs e)
        {
            ScrollOutput = btnScrollOutput.IsChecked.Value;
        }

        private void RibbonWin_Loaded(object sender, RoutedEventArgs e)
        {
            Grid child = VisualTreeHelper.GetChild((DependencyObject)sender, 0) as Grid;
            if (child != null)
            {
                child.RowDefinitions[0].Height = new GridLength(0);
                child.RowDefinitions[1].Height = new GridLength(0);
            }
        }

        private void BtnDomate_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://www.paypal.com/cgi-bin/webscr" +
    "?cmd=" + "_donations" +
    "&business=" + "cinchoofrx@gmail.com" +
    "&lc=" + "US" +
    "&item_name=" + "ChoEazyCopy Donation" +
    "&currency_code=" + "USD" +
    "&bn=" + "PP%2dDonationsBF";

            System.Diagnostics.Process.Start(url);
        }
    }

    public class BoolInverterConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            return value;
        }

        #endregion
    }
}
