using Cinchoo.Core;
using Cinchoo.Core.Configuration;
using Cinchoo.Core.Diagnostics;
using Cinchoo.Core.Win32.Dialogs;
using Cinchoo.Core.WPF;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace ChoEazyCopy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        const string NEW_SETTING_FILE_NAME = "<NEW>";

        #region Instance Members (Private)

        internal static string Caption;
        private DispatcherTimer _dispatcherTimer;
        private Thread _mainUIThread;
        private Thread _processFilesThread;
        private ChoAppSettings _appSettings;
        private bool IsStopping = false;
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
                RaisePropertyChanged(nameof(SettingsFilePath));
                SettingsFileName = Path.GetFileNameWithoutExtension(_settingsFilePath);
                SetTitle();
            }
        }
        private string _settingsFileName = null;
        public string SettingsFileName
        {
            get { return _settingsFileName; }
            private set
            {
                _settingsFileName = value;
                RaisePropertyChanged(nameof(SettingsFileName));
            }
        }

        public bool DateCreated
        {
            get { return Properties.Settings.Default.DateCreated; }
            set
            {
                Properties.Settings.Default.DateCreated = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(DateCreated));
            }
        }

        public bool DateModified
        {
            get { return Properties.Settings.Default.DateModified; }
            set
            {
                Properties.Settings.Default.DateModified = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(DateModified));
            }
        }

        public bool KeepDateCreated
        {
            get { return Properties.Settings.Default.KeepDateCreated; }
            set
            {
                Properties.Settings.Default.KeepDateCreated = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(KeepDateCreated));
            }
        }

        public bool KeepDateModified
        {
            get { return Properties.Settings.Default.KeepDateModified; }
            set
            {
                Properties.Settings.Default.KeepDateModified = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(KeepDateModified));
            }
        }

        public bool SizeColumnToFit
        {
            get { return Properties.Settings.Default.SizeColumnToFit; }
            set
            {
                Properties.Settings.Default.SizeColumnToFit = value;
                Properties.Settings.Default.Save();
            }
        }

        private string _propertyGridTooltip;
        public string PropertyGridTooltip
        {
            get { return _propertyGridTooltip; }
            set
            {
                _propertyGridTooltip = value;
                RaisePropertyChanged(nameof(PropertyGridTooltip));
            }
        }
        private bool _cloneTaskEnabled;
        public bool CloneTaskEnabled
        {
            get { return _cloneTaskEnabled; }
            set
            {
                _cloneTaskEnabled = value;
                RaisePropertyChanged(nameof(CloneTaskEnabled));
            }
        }
        private bool _deleteTaskEnabled;
        public bool DeleteTaskEnabled
        {
            get { return _deleteTaskEnabled; }
            set
            {
                _deleteTaskEnabled = value;
                RaisePropertyChanged(nameof(DeleteTaskEnabled));
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

        private BackupTaskInfo _selectedBackupTaskItem;
        public BackupTaskInfo SelectedBackupTaskItem
        {
            get { return _selectedBackupTaskItem; }
            set
            {
                _selectedBackupTaskItem = value;
                RaisePropertyChanged(nameof(SelectedBackupTaskItem));
            }
        }
        private string _selectedBackupTaskFilePath;
        public string SelectedBackupTaskFilePath
        {
            get { return _selectedBackupTaskFilePath; }
            set
            {
                var origValue = _selectedBackupTaskFilePath;
                if (value == _selectedBackupTaskFilePath)
                    return;

                _selectedBackupTaskFilePath = value;
                if (!SaveSettings())
                {
                    OpenSettingsFile(_selectedBackupTaskFilePath);
                    RaisePropertyChanged(nameof(SelectedBackupTaskFilePath));
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            _selectedBackupTaskFilePath = origValue;
                            RaisePropertyChanged(nameof(SelectedBackupTaskFilePath));
                        }),
                        DispatcherPriority.ContextIdle,
                        null
                    );
                }
            }
        }
        private ObservableCollection<BackupTaskInfo> _backupTaskInfos = new ObservableCollection<BackupTaskInfo>();
        public ObservableCollection<BackupTaskInfo> BackupTaskInfos
        {
            get { return _backupTaskInfos; }
            set
            {
                _backupTaskInfos = value;
                RaisePropertyChanged(nameof(BackupTaskInfos));
            }
        }

        private string _backupTaskDirectory;
        public string BackupTaskDirectory
        {
            get { return _backupTaskDirectory; }
            set
            {
                _backupTaskDirectory = value;
                RaisePropertyChanged(nameof(BackupTaskDirectory));
                ReloadBackupTasks(_backupTaskDirectory);
            }
        }

        private bool _backupTaskDirStatus = true;
        public bool BackupTaskDirStatus
        {
            get { return _backupTaskDirStatus; }
            set
            {
                _backupTaskDirStatus = value;
                RaisePropertyChanged(nameof(BackupTaskDirStatus));
            }
        }

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

        private bool _rememberWindowSizeAndPosition = true;
        public bool RememberWindowSizeAndPosition
        {
            get { return _rememberWindowSizeAndPosition; }
            set
            {
                _rememberWindowSizeAndPosition = value;
                RaisePropertyChanged(nameof(RememberWindowSizeAndPosition));
            }
        }

        private ChoAppSettings _bakAppSettings;
        public ChoAppSettings BakAppSettings
        {
            get { return _bakAppSettings; }
            set
            {
                _bakAppSettings = value;
                RaisePropertyChanged(nameof(BakAppSettings));
            }
        }

        public ChoAppSettings AppSettings
        {
            get { return _appSettings; }
            set
            {
                _appSettings = value;
                BakAppSettings = value;
                RaisePropertyChanged(nameof(AppSettings));
            }
        }

        private string _sourceDirTooltip = "Source directory.";
        public string SourceDirTooltip
        {
            get { return _sourceDirTooltip; }
            set
            {
                _sourceDirTooltip = value;
                RaisePropertyChanged(nameof(SourceDirTooltip));
            }
        }

        private string _backupTaskDirTooltip = "Backup Tasks directory.";
        public string BackupTaskDirTooltip
        {
            get { return _backupTaskDirTooltip; }
            set
            {
                _backupTaskDirTooltip = value;
                RaisePropertyChanged(nameof(BackupTaskDirTooltip));
            }
        }

        private bool _sourceDirStatus = true;
        public bool SourceDirStatus
        {
            get { return _sourceDirStatus; }
            set
            {
                _sourceDirStatus = value;
                RaisePropertyChanged(nameof(SourceDirStatus));
            }
        }

        private bool _showOutputLineNo = true;
        public bool ShowOutputLineNo
        {
            get { return _showOutputLineNo; }
            set
            {
                if (_showOutputLineNo != value)
                {
                    var appSettings = AppSettings;
                    if (appSettings != null)
                    {
                        IsDirty = true;
                        appSettings.ShowOutputLineNumbers = value;
                    }
                    _showOutputLineNo = value;
                    RaisePropertyChanged(nameof(ShowOutputLineNo));
                }
            }
        }

        private bool _listOnly = true;
        public bool ListOnly
        {
            get { return _listOnly; }
            set
            {
                if (_listOnly != value)
                {
                    var appSettings = AppSettings;
                    if (appSettings != null)
                    {
                        IsDirty = true;
                        appSettings.ListOnly = value;
                    }
                    _listOnly = value;
                    RaisePropertyChanged(nameof(ListOnly));
                }
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
            //Title = "{0} (v{1})".FormatString(Title, Assembly.GetEntryAssembly().GetName().Version);
            SetTitle();
        }

        private void SetTitle()
        {
            var settingsFileName = SettingsFileName.IsNullOrWhiteSpace() ? String.Empty : $" - {SettingsFileName}";

            var attr = typeof(MainWindow).Assembly.GetCustomAttribute<ChoAssemblyBetaVersionAttribute>();
            if (attr != null && attr.Version.IsNullOrWhiteSpace())
                Title = $"{Caption} (v{Assembly.GetEntryAssembly().GetName().Version}){settingsFileName}";
            else
                Title = $"{Caption} (v{Assembly.GetEntryAssembly().GetName().Version} - {attr.Version}){settingsFileName}";
        }

        private void MyWindow_Loaded(object sender1, RoutedEventArgs e1)
        {
            _bindObj = new ChoWPFBindableConfigObject<ChoAppSettings>();
            _appSettings = _bindObj.UnderlyingSource;
            _appSettings.Init();
            if (!SettingsFilePath.IsNullOrWhiteSpace() && File.Exists(SettingsFilePath))
                _appSettings.LoadXml(File.ReadAllText(SettingsFilePath));
            else
                _appSettings.Reset();

            DataContext = this;
            _mainUIThread = Thread.CurrentThread;

            btnNewFile_Click(null, null);

            _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            _dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            _dispatcherTimer.Start();

            string _ = _appSettings.SourceDirectory;
            ChoShellExtCmdLineArgs cmdLineArgs = new ChoShellExtCmdLineArgs();
            if (!cmdLineArgs.Directory.IsNullOrWhiteSpace())
                _appSettings.SourceDirectory = cmdLineArgs.Directory;

            _showOutputLineNo = _appSettings.ShowOutputLineNumbers;
            _listOnly = _appSettings.ListOnly;

            var up = new ChoUserPreferences();
            RememberWindowSizeAndPosition = up.RememberWindowSizeAndPosition;
            ScrollOutput = up.ScrollOutput;

            if (up.RememberWindowSizeAndPosition)
            {
                this.Height = up.WindowHeight;
                this.Width = up.WindowWidth;
                this.Top = up.WindowTop;
                this.Left = up.WindowLeft;
                this.WindowState = up.WindowState;
            }

            BackupTaskDirectory = up.BackupTaskDirectory;

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
                             Caption, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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
                     txtRoboCopyCmdEx.Text = _appSettings.GetCmdLineTextEx();
                     if (e.PropertyName == "Comments")
                         RaisePropertyChanged("AppSettings");
                 }));
        }

        private void _appSettings_AfterConfigurationObjectLoaded(object sender, ChoConfigurationObjectEventArgs e)
        {
            if (_wndLoaded)
            {
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                     new Action(() =>
                     {
                         this.DataContext = null;
                         this.DataContext = this;
                         txtRoboCopyCmd.Text = _appSettings.GetCmdLineText();
                         txtRoboCopyCmdEx.Text = _appSettings.GetCmdLineTextEx();
                         IsDirty = false;
                     }));
            }
            else
            {
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(() =>
                     {
                         txtRoboCopyCmd.Text = _appSettings.GetCmdLineText();
                         txtRoboCopyCmdEx.Text = _appSettings.GetCmdLineTextEx();
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
            if (_msgBuffer.Length > 0)
            {
                lock (_msgBufferLock)
                {
                    if (_msgBuffer.Length > 0)
                    {
                        txtStatus.AppendText(_msgBuffer.ToString());
                        if (ScrollOutput)
                            txtStatus.ScrollToEnd();
                        _msgBuffer.Clear();
                    }
                }
            }
            if (IsStopping)
                return;

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
                {
                    btnRun.IsEnabled = false;
                }
            }
            if (txtSourceDirectory.Text.IsNullOrWhiteSpace()
                    || Directory.Exists(txtSourceDirectory.Text)
                )
            {
                SourceDirTooltip = "Source directory.";
                SourceDirStatus = true;
            }
            else
            {
                SourceDirTooltip = $"Direcory not exists.";
                SourceDirStatus = false;
            }
            if (BackupTaskDirectory.IsNullOrWhiteSpace()
                    || Directory.Exists(BackupTaskDirectory)
            )
            {
                BackupTaskDirTooltip = "Backup Tasks directory.";
            }
            else
            {
                BackupTaskDirTooltip = $"Direcory not exists.";
            }

            if (BackupTaskDirectory.IsNullOrWhiteSpace()
                    || Directory.Exists(BackupTaskDirectory)
                )
            {
                BackupTaskDirStatus = true;
            }
            else
            {
                BackupTaskDirStatus = false;
            }

            DeleteTaskEnabled = CloneTaskEnabled = !SelectedBackupTaskFilePath.IsNullOrWhiteSpace();

            //tabControlPanel.IsEnabled = !IsRunning;
            grpBackupTasks.IsEnabled = !IsRunning;
            pgAppSettings.Visibility = IsRunning ? Visibility.Collapsed : Visibility.Visible;
            txtPropertyGridWaterMark.Visibility = !IsRunning ? Visibility.Collapsed : Visibility.Visible;
            if (IsRunning)
            {
                //BakAppSettings = null;
                PropertyGridTooltip = "Grid will be hidden while task running.";
            }
            else
            {
                //BakAppSettings = AppSettings;
                PropertyGridTooltip = String.Empty;
            }

            //DO NOT UNCOMMENT - pgAppSettings (ExtendedPropertyGrid) - setting to disabled causes abnormal application termination
            //pgAppSettings.IsEnabled = !IsRunning;

            btnStop.IsEnabled = IsRunning;
            btnNewFile.IsEnabled = !IsRunning;
            btnOpenFile.IsEnabled = !IsRunning;
            btnSaveFile.IsEnabled = !IsRunning && IsDirty;
            btnSaveAsFile.IsEnabled = !IsRunning;
            btnClear.IsEnabled = !IsRunning && txtStatus.Text.Length > 0;

            if (_processFilesThread != null && _processFilesThread.IsAlive)
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
            ChoAppSettings appSettings = cmd as ChoAppSettings; // cmd.ToString();
            if (appSettings == null)
                return;

            try
            {
                IsRunning = true;
#if _DELAY_RUN_
                Thread.Sleep(10 * 1000);
#endif
                _roboCopyManager = new ChoRoboCopyManager();
                _roboCopyManager.Status += (sender, e) => SetStatusMsg(e.Message);
                _roboCopyManager.AppStatus += (sender, e) => UpdateStatus(e.Message, e.Tag.ToNString());

                _roboCopyManager.Process(appSettings.RoboCopyFilePath, appSettings.GetCmdLineParams(), appSettings);
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
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

        private readonly StringBuilder _msgBuffer = new StringBuilder();
        private readonly object _msgBufferLock = new object();

        private void SetStatusMsg(string msg)
        {
            if (msg != Environment.NewLine && msg.IsNullOrWhiteSpace()) return;

            lock (_msgBufferLock)
            {
                _msgBuffer.Append(msg);
            }

            /*
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
            */
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
            ChoFileMoveAttributes value = ChoFileMoveAttributes.None;
            if (Enum.TryParse<ChoFileMoveAttributes>(_appSettings.MoveFilesAndDirectories.ToNString(), out value))
            {
                switch (value)
                {
                    case ChoFileMoveAttributes.MoveFilesOnly:
                        if (MessageBox.Show("Are you sure you wish to remove original file{s}? This CANNOT be undone!", Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                            return;
                        break;
                    case ChoFileMoveAttributes.MoveDirectoriesAndFiles:
                        if (MessageBox.Show("Are you sure you wish to remove original file{s} / folder(s)? This CANNOT be undone!", Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                            return;
                        break;
                    default:
                        break;
                }
            }

            _processFilesThread = new Thread(new ParameterizedThreadStart(ProcessFiles));
            _processFilesThread.IsBackground = true;
            _processFilesThread.Start(_appSettings);
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(@"Are you sure you want to stop the operation?", Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop, MessageBoxResult.No)
                == MessageBoxResult.No)
                return;

            IsStopping = true;
            btnStop.IsEnabled = false;
            Task.Run(() =>
            {
                ChoRoboCopyManager roboCopyManager = _roboCopyManager;
                if (roboCopyManager != null)
                {
                    roboCopyManager.Cancel();
                }

                Thread processFilesThread = _processFilesThread;
                if (processFilesThread != null)
                {
                    try
                    {
                        processFilesThread.Abort();
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                    }
                    _processFilesThread = null;
                }
                IsStopping = false;
            });
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
                var xml = _appSettings.ToXml();
                File.WriteAllText(SettingsFilePath, xml);
                IsDirty = false;
                ReloadBackupTasks();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed saving settings to file. {0}".FormatString(ex.Message), Caption, MessageBoxButton.OK, MessageBoxImage.Error);
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
                OpenSettingsFile(dlg.FileName);
            }
        }

        private void OpenSettingsFile(string settingsFileName)
        {
            using (var x = new ChoWPFWaitCursor())
            {
                _isNewFileOp = true;
                SettingsFilePath = settingsFileName;
                UnregisterEvents();
                _appSettings.Reset();
                if (File.Exists(SettingsFilePath))
                    _appSettings.LoadXml(File.ReadAllText(SettingsFilePath));
                else
                    btnNewFile_Click(null, null);
                RegisterEvents();
                ShowOutputLineNo = _appSettings.ShowOutputLineNumbers;
                ListOnly = _appSettings.ListOnly;
                txtStatus.Text = String.Empty;
                //this.DataContext = null;
                //this.DataContext = this;
                IsDirty = false;
                _isNewFileOp = false;
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
                ShowOutputLineNo = false;
                ListOnly = false;
                txtStatus.Text = String.Empty;
                UnregisterEvents();
                _appSettings.Reset();
                RegisterEvents();
                //this.DataContext = null;
                //this.DataContext = this;
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

            if (!e.Cancel)
            {
                var up = new ChoUserPreferences();
                if (RememberWindowSizeAndPosition)
                {
                    up.WindowHeight = this.Height;
                    up.WindowWidth = this.Width;
                    up.WindowTop = this.Top;
                    up.WindowLeft = this.Left;
                    up.WindowState = this.WindowState;
                }
                up.RememberWindowSizeAndPosition = RememberWindowSizeAndPosition;
                up.ScrollOutput = ScrollOutput;
                up.BackupTaskDirectory = BackupTaskDirectory;
                up.Save();
            }
        }

        private void txtRoboCopyCmd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isNewFileOp)
                IsDirty = true;
        }

        private void txtRoboCopyCmdEx_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isNewFileOp)
                IsDirty = true;
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

        private void BtnDonate_Click(object sender, RoutedEventArgs e)
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

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/robocopy");
            }
            catch { }
        }

        private void btnSwapDir_Click(object sender, RoutedEventArgs e)
        {
            var appSettings = AppSettings;
            if (appSettings == null)
                return;

            var srcDir = appSettings.SourceDirectory;
            var destDir = appSettings.DestDirectory;

            txtSourceDirectory.Text = destDir;
            txtDestDirectory.Text = srcDir;
        }

        private void btnRefreshBackupTasks_Click(object sender, RoutedEventArgs e)
        {
            ReloadBackupTasks(BackupTaskDirectory);
        }

        private void btnBackupTaskDirectory_Click(object sender, RoutedEventArgs e)
        {
            ChoFolderBrowserDialog dlg1 = new ChoFolderBrowserDialog
            {
                Description = "Choose Backup Tasks folder...",
                ShowNewFolderButton = true,
                ShowEditBox = true,
                ShowBothFilesAndFolders = false,
                NewStyle = true,
                SelectedPath = (System.IO.Directory.Exists(BackupTaskDirectory)) ? BackupTaskDirectory : "",
                ShowFullPathInEditBox = false,
            };
            dlg1.RootFolder = System.Environment.SpecialFolder.MyComputer;

            var result = dlg1.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(dlg1.SelectedPath))
                    BackupTaskDirectory = dlg1.SelectedPath;
                else
                    BackupTaskDirectory = System.IO.Path.GetDirectoryName(dlg1.SelectedPath);

                var up = new ChoUserPreferences();
                up.BackupTaskDirectory = BackupTaskDirectory;
                up.Save();

                ReloadBackupTasks(BackupTaskDirectory);
            }
        }

        private void ReloadBackupTasks()
        {
            ReloadBackupTasks(BackupTaskDirectory);
        }

        private bool _isBackupTasksLoading = false;
        private object _padLock = new object();
        private void ReloadBackupTasks(string backupTasksDir)
        {
            if (Application.Current == null)
                return;

            if (_isBackupTasksLoading)
                return;

            lock (_padLock)
            {
                if (_isBackupTasksLoading)
                    return;
                Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    try
                    {
                        var selectedBackupTaskInfo = SelectedBackupTaskFilePath;

                        _isBackupTasksLoading = true;
                        BackupTaskInfos.Clear();
                        if (backupTasksDir.IsNullOrWhiteSpace() || !Directory.Exists(backupTasksDir))
                            return;

                        foreach (var fi in Directory.GetFiles(backupTasksDir, $"*{AppHost.AppFileExt}").Take(1000)
                            .Select(f => new BackupTaskInfo(f)))
                        {
                            BackupTaskInfos.Add(fi);
                        }
                        if (selectedBackupTaskInfo.IsNullOrWhiteSpace()
                            || !BackupTaskInfos.Select(f => f.FilePath == selectedBackupTaskInfo).Any())
                        {
                            var fi = BackupTaskInfos.FirstOrDefault();
                            selectedBackupTaskInfo = fi != null ? fi.FilePath : null;
                        }

                        SelectedBackupTaskFilePath = selectedBackupTaskInfo;
                    }
                    finally
                    {
                        _isBackupTasksLoading = false;
                    }
                }),
                DispatcherPriority.ContextIdle,
                null
            );
            }
        }

        private void mnuCloneTask_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedBackupTaskItem != null)
                CloneTask();
        }

        private void mnuDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedBackupTaskItem != null)
                DeleteTask();
        }

        private void btnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            DeleteTask();
        }

        private void DeleteTask()
        {
            if (SelectedBackupTaskFilePath.IsNullOrWhiteSpace())
                return;

            if (MessageBox.Show($"Are you sure you want to delete `{Path.GetFileName(SelectedBackupTaskFilePath)}` task?",
                Caption, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(SelectedBackupTaskFilePath);
                    //ReloadBackupTasks();
                    var index = BackupTaskInfos.ToList().FindIndex(f => f.FilePath == SelectedBackupTaskFilePath);
                    BackupTaskInfos.RemoveAt(index);
                    if (index - 1 >= 0)
                    {
                        SelectedBackupTaskFilePath = BackupTaskInfos[index - 1].FilePath;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete `{Path.GetFileName(SelectedBackupTaskFilePath)}` task. {ex.Message}",
                        Caption, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetNextCloneTaskFileName(string taskFilePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(taskFilePath);
            string dirName = Path.GetDirectoryName(taskFilePath);

            int index = 0;
            while (true)
            {
                var newFilePath = Path.Combine(dirName, $"{fileName}_{index}{AppHost.AppFileExt}");
                if (!File.Exists(newFilePath))
                    return newFilePath;

                index++;
            }
        }

        private void btnCloneTask_Click(object sender, RoutedEventArgs e)
        {
            CloneTask();
        }

        private void CloneTask()
        {
            if (SelectedBackupTaskFilePath.IsNullOrWhiteSpace())
                return;

            var clonedTaskFilePath = GetNextCloneTaskFileName(SelectedBackupTaskFilePath);
            try
            {
                var bfi = new BackupTaskInfo(SelectedBackupTaskFilePath);
                File.Copy(SelectedBackupTaskFilePath, clonedTaskFilePath, true);
                if (KeepDateCreated)
                    File.SetCreationTime(clonedTaskFilePath, bfi.CreatedDate);
                if (KeepDateModified)
                    File.SetLastWriteTime(clonedTaskFilePath, bfi.ModifiedDate);

                //ReloadBackupTasks();
                BackupTaskInfos.Add(new BackupTaskInfo(clonedTaskFilePath));
                SelectedBackupTaskFilePath = clonedTaskFilePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clone `{Path.GetFileName(SelectedBackupTaskFilePath)}` task. {ex.Message}",
                    Caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class BackupTaskInfo
    {
        public string TaskName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public BackupTaskInfo(string filePath)
        {
            var fi = new FileInfo(filePath);

            TaskName = Path.GetFileNameWithoutExtension(filePath);
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            CreatedDate = fi.CreationTime;
            ModifiedDate = fi.LastWriteTime;
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
    public class BoolToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (bool)value;
            if (val)
                return SystemColors.WindowBrush;
            else
                return Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToForegroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (bool)value;
            if (val)
                return SystemColors.WindowTextBrush;
            else
                return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class MathConverter : IValueConverter
    {
        private static readonly char[] _allOperators = new[] { '+', '-', '*', '/', '%', '(', ')' };

        private static readonly List<string> _grouping = new List<string> { "(", ")" };
        private static readonly List<string> _operators = new List<string> { "+", "-", "*", "/", "%" };

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Parse value into equation and remove spaces
            var mathEquation = parameter as string;
            mathEquation = mathEquation.Replace(" ", "");
            mathEquation = mathEquation.Replace("@VALUE", value.ToString());

            // Validate values and get list of numbers in equation
            var numbers = new List<double>();
            double tmp;

            foreach (string s in mathEquation.Split(_allOperators))
            {
                if (s != string.Empty)
                {
                    if (double.TryParse(s, out tmp))
                    {
                        numbers.Add(tmp);
                    }
                    else
                    {
                        // Handle Error - Some non-numeric, operator, or grouping character found in string
                        throw new InvalidCastException();
                    }
                }
            }

            // Begin parsing method
            EvaluateMathString(ref mathEquation, ref numbers, 0);

            // After parsing the numbers list should only have one value - the total
            return numbers[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        // Evaluates a mathematical string and keeps track of the results in a List<double> of numbers
        private void EvaluateMathString(ref string mathEquation, ref List<double> numbers, int index)
        {
            // Loop through each mathemtaical token in the equation
            string token = GetNextToken(mathEquation);

            while (token != string.Empty)
            {
                // Remove token from mathEquation
                mathEquation = mathEquation.Remove(0, token.Length);

                // If token is a grouping character, it affects program flow
                if (_grouping.Contains(token))
                {
                    switch (token)
                    {
                        case "(":
                            EvaluateMathString(ref mathEquation, ref numbers, index);
                            break;

                        case ")":
                            return;
                    }
                }

                // If token is an operator, do requested operation
                if (_operators.Contains(token))
                {
                    // If next token after operator is a parenthesis, call method recursively
                    string nextToken = GetNextToken(mathEquation);
                    if (nextToken == "(")
                    {
                        EvaluateMathString(ref mathEquation, ref numbers, index + 1);
                    }

                    // Verify that enough numbers exist in the List<double> to complete the operation
                    // and that the next token is either the number expected, or it was a ( meaning
                    // that this was called recursively and that the number changed
                    if (numbers.Count > (index + 1) &&
                        (double.Parse(nextToken) == numbers[index + 1] || nextToken == "("))
                    {
                        switch (token)
                        {
                            case "+":
                                numbers[index] = numbers[index] + numbers[index + 1];
                                break;
                            case "-":
                                numbers[index] = numbers[index] - numbers[index + 1];
                                break;
                            case "*":
                                numbers[index] = numbers[index] * numbers[index + 1];
                                break;
                            case "/":
                                numbers[index] = numbers[index] / numbers[index + 1];
                                break;
                            case "%":
                                numbers[index] = numbers[index] % numbers[index + 1];
                                break;
                        }
                        numbers.RemoveAt(index + 1);
                    }
                    else
                    {
                        // Handle Error - Next token is not the expected number
                        throw new FormatException("Next token is not the expected number");
                    }
                }

                token = GetNextToken(mathEquation);
            }
        }

        // Gets the next mathematical token in the equation
        private string GetNextToken(string mathEquation)
        {
            // If we're at the end of the equation, return string.empty
            if (mathEquation == string.Empty)
            {
                return string.Empty;
            }

            // Get next operator or numeric value in equation and return it
            string tmp = "";
            foreach (char c in mathEquation)
            {
                if (_allOperators.Contains(c))
                {
                    return (tmp == "" ? c.ToString() : tmp);
                }
                else
                {
                    tmp += c;
                }
            }

            return tmp;
        }
    }

    public class ExtendedPropertyGrid : PropertyGrid
    {
        protected override void OnFilterChanged(string oldValue, string newValue)
        {
            newValue = newValue.ToLower();
            CollectionViewSource.GetDefaultView((object)this.Properties).Filter
                = (item => (item as PropertyItem).DisplayName.IndexOf(newValue, StringComparison.InvariantCultureIgnoreCase) >= 0
                || (item as PropertyItem).Description.IndexOf(newValue, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }
    }


    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
    public class ChoAssemblyBetaVersionAttribute : Attribute
    {
        public string Version { get; set; }
        public ChoAssemblyBetaVersionAttribute(string text)
        {
            Version = text;
        }
    }
}
