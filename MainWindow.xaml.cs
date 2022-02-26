using Cinchoo.Core;
using Cinchoo.Core.Configuration;
using Cinchoo.Core.Diagnostics;
using Cinchoo.Core.Win32.Dialogs;
using Cinchoo.Core.WPF;
using MahApps.Metro;
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
using System.Runtime.CompilerServices;
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
        private ChoRoboCopyManager _roboCopyManager = null;
        private DispatcherTimer _dispatcherTimer;
        private Thread _mainUIThread;
        private Thread _processFilesThread;
        private ChoAppSettings _appSettings;
        private GridViewColumnHeader listViewSortCol = null;
        private ChoSortAdorner listViewSortAdorner = null;
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

        public bool IsAdminMode
        {
            get;
            set;
        }
        public object ControlMouseOverBackgroundBrush
        {
            get
            {
                return ChoAppTheme.ControlMouseOverBackgroundBrush;
            }
        }

        public object ControlBackgroundBrush
        {
            get 
            {
                return ChoAppTheme.ControlBackgroundBrush;
            }
        }

        public object ControlForegroundBrush
        {
            get
            {
                return ChoAppTheme.ControlForegroundBrush;
            }
        }

        public object TextBoxFocusBorderBrush
        {
            get
            {
                return ChoAppTheme.TextBoxFocusBorderBrush;
            }
        }
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
                if (_settingsFilePath.IsNullOrWhiteSpace())
                    SettingsFileName = null;
                else
                    SettingsFileName = Path.GetFileNameWithoutExtension(_settingsFilePath);
                SetTitle();
            }
        }
        private string _settingsFileName = null;
        public string SettingsFileName
        {
            get { return _settingsFileName.IsNullOrWhiteSpace() ? NEW_SETTING_FILE_NAME : _settingsFileName; }
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
                RaisePropertyChanged(nameof(SizeColumnToFit));
            }
        }

        public bool SizeAllColumnsToFit
        {
            get { return Properties.Settings.Default.SizeAllColumnsToFit; }
            set
            {
                Properties.Settings.Default.SizeAllColumnsToFit = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(SizeAllColumnsToFit));
            }
        }

        public bool ConfirmOnDelete
        {
            get { return Properties.Settings.Default.ConfirmOnDelete; }
            set
            {
                Properties.Settings.Default.ConfirmOnDelete = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(ConfirmOnDelete));
            }
        }

        public bool WatchForChanges
        {
            get { return Properties.Settings.Default.WatchForChanges; }
            set
            {
                Properties.Settings.Default.WatchForChanges = value;
                Properties.Settings.Default.Save();
                WatchBackupTasksDirectory();
                RaisePropertyChanged(nameof(WatchForChanges));
            }
        }

        public double TaskNameColumnWidth
        {
            get { return Properties.Settings.Default.TaskNameColumnWidth; }
            set
            {
                Properties.Settings.Default.TaskNameColumnWidth = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(TaskNameColumnWidth));
            }
        }

        public double DateCreatedColumnWidth
        {
            get { return Properties.Settings.Default.DateCreatedColumnWidth; }
            set
            {
                Properties.Settings.Default.DateCreatedColumnWidth = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(DateCreatedColumnWidth));
            }
        }

        public double DateModifiedColumnWidth
        {
            get { return Properties.Settings.Default.DateModifiedColumnWidth; }
            set
            {
                Properties.Settings.Default.DateModifiedColumnWidth = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(DateModifiedColumnWidth));
            }
        }

        public bool BackupTaskTabActiveAtOpen
        {
            get { return Properties.Settings.Default.BackupTaskTabActiveAtOpen; }
            set
            {
                Properties.Settings.Default.BackupTaskTabActiveAtOpen = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(BackupTaskTabActiveAtOpen));
            }
        }

        public bool ControlPanelMinimized
        {
            get { return Properties.Settings.Default.ControlPanelMinimized; }
            set
            {
                Properties.Settings.Default.ControlPanelMinimized = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(ControlPanelMinimized));
            }
        }

        public GridLength ControlPanelWidth
        {
            get { return Properties.Settings.Default.ControlPanelWidth; }
            set
            {
                Properties.Settings.Default.ControlPanelWidth = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(nameof(ControlPanelWidth));
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string propName = null)
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

                try
                {
                    _selectedBackupTaskFilePath = value;
                    if (!SaveSettings())
                    {
                        if (File.Exists(_selectedBackupTaskFilePath))
                        {
                            OpenSettingsFile(_selectedBackupTaskFilePath);
                            RaisePropertyChanged(nameof(SelectedBackupTaskFilePath));
                        }
                        else if (!_selectedBackupTaskFilePath.IsNullOrWhiteSpace())
                        {
                            MessageBox.Show($"File `{_selectedBackupTaskFilePath}` does not exists.", Caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                            NewSettingsFile(false);
                            ReloadBackupTasks();
                        }
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
                }finally
                {
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
        public bool DestDirStatus
        {
            get;
            set;
        } = true;
        private string _cmdLineText = null;
        public string CmdLineText
        {
            get { return _cmdLineText; }
            set
            {
                _cmdLineText = value;
                RaisePropertyChanged(nameof(CmdLineText));
            }
        }
        private string _cmdLineTextEx = null;
        public string CmdLineTextEx
        {
            get { return _cmdLineTextEx; }
            set
            {
                _cmdLineTextEx = value;
                RaisePropertyChanged(nameof(CmdLineTextEx));
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

            var up = new ChoUserPreferences();
            RememberWindowSizeAndPosition = up.RememberWindowSizeAndPosition;

            if (up.RememberWindowSizeAndPosition)
            {
                this.Height = up.WindowHeight;
                this.Width = up.WindowWidth;
                this.Top = up.WindowTop;
                this.Left = up.WindowLeft;
                this.WindowState = up.WindowState;
            }

            ContentRendered += (o, e) =>
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        using (var cursor = new ChoWPFWaitCursor())
                        {
                            Caption = Title;
                            //Title = "{0} (v{1})".FormatString(Title, Assembly.GetEntryAssembly().GetName().Version);
                            SetTitle();
                            LoadWindow();
                        }
                    }),
                    DispatcherPriority.ContextIdle,
                    null
                );
            };
        }

        private void LoadWindow()
        {
            _appSettings = new ChoAppSettings();
            if (!SettingsFilePath.IsNullOrWhiteSpace() && File.Exists(SettingsFilePath))
                _appSettings.LoadXml(File.ReadAllText(SettingsFilePath));
            else
            {
                _appSettings.Reset();
                NewSettingsFile();
            }

            DataContext = this;
            _mainUIThread = Thread.CurrentThread;


            _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            _dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            _dispatcherTimer.Start();

            string _ = _appSettings.SourceDirectory;
            ChoShellExtCmdLineArgs cmdLineArgs = new ChoShellExtCmdLineArgs();
            if (!cmdLineArgs.Directory.IsNullOrWhiteSpace())
                _appSettings.SourceDirectory = cmdLineArgs.Directory;

            var up = new ChoUserPreferences();
            RememberWindowSizeAndPosition = up.RememberWindowSizeAndPosition;
            ScrollOutput = up.ScrollOutput;

            //if (up.RememberWindowSizeAndPosition)
            //{
            //    this.Height = up.WindowHeight;
            //    this.Width = up.WindowWidth;
            //    this.Top = up.WindowTop;
            //    this.Left = up.WindowLeft;
            //    this.WindowState = up.WindowState;
            //}

            _backupTaskDirectory = up.BackupTaskDirectory;
            ReloadBackupTasks(SettingsFilePath);
            RaisePropertyChanged(nameof(BackupTaskDirectory));

            grdTaskNameColumn.Width = TaskNameColumnWidth;
            ChoGridViewColumnVisibilityManager.SetGridColumnWidth(grdTaskNameColumn);

            grdDateCreatedColumn.Width = DateCreatedColumnWidth;
            ChoGridViewColumnVisibilityManager.SetGridColumnWidth(grdDateCreatedColumn);

            grdDateModifiedColumn.Width = DateModifiedColumnWidth;
            ChoGridViewColumnVisibilityManager.SetGridColumnWidth(grdDateModifiedColumn);

            tabBackupTasks.IsSelected = BackupTaskTabActiveAtOpen;
            expControlPanel.IsExpanded = ControlPanelMinimized;
            
            IsDirty = false;

            if (WatchForChanges)
                WatchBackupTasksDirectory();
            txtRoboCopyCmd.Text = _appSettings.GetCmdLineText();
            txtRoboCopyCmdEx.Text = _appSettings.GetCmdLineTextEx();

            txtRoboCopyCmd.TextChanged += (o, e) => IsDirty = true;
            txtRoboCopyCmdEx.TextChanged += (o, e) => IsDirty = true;
            _appSettings.PropertyChanged += (o, e) =>
            {
                txtRoboCopyCmd.Text = _appSettings.GetCmdLineText();
                txtRoboCopyCmdEx.Text = _appSettings.GetCmdLineTextEx();
            };
            IsAdminMode = AppHost.IsRunAsAdmin();
            if (IsAdminMode)
            {
                mnuRunasAdministrator.Visibility = Visibility.Collapsed;
                mnuRegisterShellExtensions.Visibility = Visibility.Visible;
                mnuUnregisterShellExtensions.Visibility = Visibility.Visible;
            }
            else
            {
                mnuRunasAdministrator.Visibility = Visibility.Visible;
                mnuRegisterShellExtensions.Visibility = Visibility.Collapsed;
                mnuUnregisterShellExtensions.Visibility = Visibility.Collapsed;
            }
        }
        private void SetTitle()
        {
            var settingsFileName = String.Empty; // SettingsFileName.IsNullOrWhiteSpace() ? String.Empty : $" - {SettingsFileName}";

            var attr = typeof(MainWindow).Assembly.GetCustomAttribute<ChoAssemblyBetaVersionAttribute>();
            if (attr != null && attr.Version.IsNullOrWhiteSpace())
                Title = $"{Caption} (v{Assembly.GetEntryAssembly().GetName().Version}){settingsFileName}";
            else
                Title = $"{Caption} (v{Assembly.GetEntryAssembly().GetName().Version} - {attr.Version}){settingsFileName}";
        }

        private void Window_Loaded(object sender1, RoutedEventArgs e1)
        {
        }

        public void RefreshWindow()
        {
            RaisePropertyChanged(nameof(ControlMouseOverBackgroundBrush));
            RaisePropertyChanged(nameof(ControlBackgroundBrush));
            RaisePropertyChanged(nameof(ControlForegroundBrush));
            RaisePropertyChanged(nameof(TextBoxFocusBorderBrush));
            RaisePropertyChanged(nameof(SourceDirStatus));
            RaisePropertyChanged(nameof(DestDirStatus));
            RaisePropertyChanged(nameof(BackupTaskDirStatus));
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
                SettingsFilePath = settingsFileName;
                //_appSettings.Reset();
                if (File.Exists(SettingsFilePath))
                    _appSettings.LoadXml(File.ReadAllText(SettingsFilePath));
                //else
                //    btnNewFile_Click(null, null);
                txtStatus.Text = String.Empty;
                IsDirty = false;
            }
        }

        private void btnNewFile_Click(object sender, RoutedEventArgs e)
        {
            if (SaveSettings())
                return;

            NewSettingsFile();
        }

        private void NewSettingsFile(bool reset = true)
        {
            using (var x = new ChoWPFWaitCursor())
            {
                SettingsFilePath = null;
                txtSourceDirectory.Text = String.Empty;
                txtDestDirectory.Text = String.Empty;
                txtStatus.Text = String.Empty;
                _appSettings.Reset();
                IsDirty = false;
                //if (reset)
                //{
                //    this.DataContext = null;
                //    this.DataContext = this;
                //}
                SelectedBackupTaskItem = null;
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
                try
                {
                    BackupTaskTabActiveAtOpen = tabBackupTasks.IsSelected;
                    ControlPanelMinimized = expControlPanel.IsExpanded;

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
                catch { }
            }
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
            string url = "https://www.paypal.com/donate/?hosted_button_id=HB6J7QG73HMK8";
    //        string url = "https://www.paypal.com/cgi-bin/webscr" +
    //"?cmd=" + "_donations" +
    //"&business=" + "cinchoofrx@gmail.com" +
    //"&lc=" + "US" +
    //"&item_name=" + "ChoEazyCopy Donation" +
    //"&currency_code=" + "USD" +
    //"&bn=" + "PP%2dDonationsBF";

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

            var dir = appSettings.SourceDirectory;

            appSettings.SourceDirectory = appSettings.DestDirectory;
            appSettings.DestDirectory = dir;
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

                //ReloadBackupTasks(BackupTaskDirectory);
            }
        }

        private FileSystemWatcher _watcher;
        private void WatchBackupTasksDirectory()
        {
            if (_watcher != null)
                _watcher.Dispose();

            if (WatchForChanges)
            {
                if (Directory.Exists(BackupTaskDirectory))
                {
                    _watcher = new FileSystemWatcher();
                    _watcher.Path = BackupTaskDirectory;
                    _watcher.NotifyFilter = NotifyFilters.Attributes |
                        NotifyFilters.CreationTime |
                        NotifyFilters.FileName |
                        NotifyFilters.LastAccess |
                        NotifyFilters.LastWrite |
                        NotifyFilters.Size |
                        NotifyFilters.Security;

                    _watcher.Filter = "*.*";
                    _watcher.Deleted += (o, e) => ReloadBackupTasks();
                    _watcher.Created += (o, e) => ReloadBackupTasks();
                    _watcher.Changed += (o, e) => ReloadBackupTasks();
                    _watcher.Renamed += (o, e) => ReloadBackupTasks();
                    _watcher.EnableRaisingEvents = true;
                }
            }
        }

        private void ReloadBackupTasks(string settingsFilePath = null)
        {
            ReloadBackupTasks(BackupTaskDirectory, settingsFilePath);
        }

        private bool _isBackupTasksLoading = false;
        private object _padLock = new object();
        private void ReloadBackupTasks(string backupTasksDir, string settingsFilePath = null)
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
                        {
                            NewSettingsFile();
                            return;
                        }

                        foreach (var fi in Directory.GetFiles(backupTasksDir, $"*{AppHost.AppFileExt}").Take(1000)
                            .Select(f => new BackupTaskInfo(f)))
                        {
                            BackupTaskInfos.Add(fi);
                        }
                        //if (selectedBackupTaskInfo.IsNullOrWhiteSpace()
                        //    || !BackupTaskInfos.Select(f => f.FilePath == selectedBackupTaskInfo).Any())
                        //{
                        //    var fi = BackupTaskInfos.FirstOrDefault();
                        //    selectedBackupTaskInfo = fi != null ? fi.FilePath : null;
                        //}
                        if (!settingsFilePath.IsNullOrWhiteSpace() &&
                            BackupTaskInfos.Select(f => f.FilePath == settingsFilePath).Any())
                        {
                            SelectedBackupTaskFilePath = settingsFilePath;
                        }
                        //WatchBackupTasksDirectory();
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

        private void mnuDeleteTask_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (SelectedBackupTaskItem != null)
                    DeleteTask();
            }
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (SelectedBackupTaskItem != null)
                    CloneTask();
            }
        }

        private void btnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            DeleteTask();
        }

        private void DeleteTask()
        {
            if (SelectedBackupTaskFilePath.IsNullOrWhiteSpace())
                return;

            MessageBoxResult result = MessageBoxResult.Yes;
            if (ConfirmOnDelete)
            {
                result = MessageBox.Show($"Are you sure you want to delete `{Path.GetFileName(SelectedBackupTaskFilePath)}` task?",
                    Caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            }

            if (result == MessageBoxResult.Yes)
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

        private void TaskNameGridViewColumnHeader_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width <= 60)
            {
                e.Handled = true;
                ((GridViewColumnHeader)sender).Column.Width = 60;
            }
            ChoGridViewColumnVisibilityManager.SetGridColumnWidth((GridViewColumnHeader)sender);
            TaskNameColumnWidth = ((GridViewColumnHeader)sender).Column.Width;
        }
        private void DateCreatedGridViewColumnHeader_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!ChoGridViewColumnVisibilityManager.GetIsVisible(((GridViewColumnHeader)sender).Column))
                return;

            if (e.NewSize.Width <= 60)
            {
                e.Handled = true;
                ((GridViewColumnHeader)sender).Column.Width = 0;
                ((GridViewColumnHeader)sender).Column.Width = 60;
            }
            ChoGridViewColumnVisibilityManager.SetGridColumnWidth((GridViewColumnHeader)sender);
            DateCreatedColumnWidth = ((GridViewColumnHeader)sender).Column.Width;
        }
        private void DateModifiedGridViewColumnHeader_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!ChoGridViewColumnVisibilityManager.GetIsVisible(((GridViewColumnHeader)sender).Column))
                return;

            if (e.NewSize.Width <= 60)
            {
                e.Handled = true;
                ((GridViewColumnHeader)sender).Column.Width = 0;
                ((GridViewColumnHeader)sender).Column.Width = 60;
            }
            ChoGridViewColumnVisibilityManager.SetGridColumnWidth((GridViewColumnHeader)sender);
            DateModifiedColumnWidth = ((GridViewColumnHeader)sender).Column.Width;
        }

        private void mnuColumnToFit_Click(object sender, RoutedEventArgs e)
        {
            var selectedGridViewColumnHeader = _selectedGridViewColumnHeader;
            if (selectedGridViewColumnHeader == null)
                return;

            selectedGridViewColumnHeader.Column.Width = 0;
            selectedGridViewColumnHeader.Column.Width = Double.NaN;
            ChoGridViewColumnVisibilityManager.SetGridColumnWidth(selectedGridViewColumnHeader);
        }

        private void mnuAllColumnsToFit_Click(object sender, RoutedEventArgs e)
        {
            ChoGridViewColumnVisibilityManager.ResizeAllColumnsToFit();
        }

        private GridViewColumnHeader _selectedGridViewColumnHeader;
        private void GridViewColumnHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                _selectedGridViewColumnHeader = sender as GridViewColumnHeader;
            }
            else
            {
                _selectedGridViewColumnHeader = null;
            }
        }

        private void grdTaskNameColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            SortColumn(column);
        }

        private void grddDateCreatedColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            SortColumn(column);
        }

        private void grddDateModifiedColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            SortColumn(column);
        }

        private void SortColumn(GridViewColumnHeader column)
        {
            string sortBy = column.Tag.ToString();
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                lstBackupTasks.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            listViewSortCol = column;
            listViewSortAdorner = new ChoSortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            lstBackupTasks.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private void mnuResetExpander_Click(object sender, RoutedEventArgs e)
        {
            ControlPanelWidth = new GridLength(300);
        }

        private void btnApplicationCmds_Click(object sender, RoutedEventArgs e)
        {
            var addButton = sender as FrameworkElement;
            if (addButton != null)
            {
                addButton.ContextMenu.IsOpen = true;
            }
        }

        private void mnuLaunchNewInstance_Click(object sender, RoutedEventArgs e)
        {
            var info = new System.Diagnostics.ProcessStartInfo(ChoApplication.EntryAssemblyLocation);
            System.Diagnostics.Process.Start(info);
        }

        private void mnuRunasAdministrator_Click(object sender, RoutedEventArgs e)
        {
            AppHost.RunAsAdmin();
        }

        private void mnuRegisterShellExtensions_Click(object sender, RoutedEventArgs e)
        {
            AppHost.RegisterShellExtensions();
        }

        private void mnuUnregisterShellExtensions_Click(object sender, RoutedEventArgs e)
        {
            AppHost.UnregisterShellExtensions();
        }

        private void mnuLightTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ChangeAppStyle(Application.Current,
                            ThemeManager.GetAccent("Steel"),
                            ThemeManager.GetAppTheme("BaseLight"));

        }

        private void mnuDarkTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ChangeAppStyle(Application.Current,
                            ThemeManager.GetAccent("Steel"),
                            ThemeManager.GetAppTheme("BaseLight"));
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
                return SystemColors.WindowBrush; // ChoAppTheme.ControlBackgroundBrush;
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
                return SystemColors.WindowTextBrush; // ChoAppTheme.ControlBackgroundBrush;
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
    public class ThemeMenuItem : MenuItem
    {
        protected override void OnClick()
        {
            var ic = Parent as ItemsControl;
            if (null != ic)
            {
                var rmi = ic.Items.OfType<ThemeMenuItem>().FirstOrDefault(i => i.IsChecked);
                if (null != rmi) rmi.IsChecked = false;

                IsChecked = true;
                ChoApplicationThemeManager.Theme = this.Tag as string;
            }
            base.OnClick();
        }
    }
    public class AccentMenuItem : MenuItem
    {
        protected override void OnClick()
        {
            var ic = Parent as ItemsControl;
            if (null != ic)
            {
                var rmi = ic.Items.OfType<AccentMenuItem>().FirstOrDefault(i => i.IsChecked);
                if (null != rmi) rmi.IsChecked = false;

                IsChecked = true;
                ChoApplicationThemeManager.Accent = this.Header as string;
            }
            base.OnClick();
        }
    }

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
