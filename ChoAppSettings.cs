namespace ChoEazyCopy
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.Collections.Generic;
    using Cinchoo.Core.Configuration;
    using System.ComponentModel;
    using System.Runtime.Remoting.Contexts;
    using System.Dynamic;
    using Cinchoo.Core.Text.RegularExpressions;
    using System.Text.RegularExpressions;
    using Cinchoo.Core.Diagnostics;
    using Cinchoo.Core;
    using System.Diagnostics;
    using Cinchoo.Core.Xml.Serialization;
    using Cinchoo.Core.IO;
    using System.IO;
    using System.Xml.Serialization;
    using System.Windows;
    using Cinchoo.Core.WPF;
    using System.Windows.Data;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using System.Runtime.CompilerServices;
    using System.Reflection;
    using SoftFluent.Windows;

    #endregion NameSpaces

    [Flags]
    public enum ChoCopyFileFlags
    {
        [Description("")]
        None = 0,
        [Description("D")]
        Data = 1,
        [Description("A")]
        Attributes = 2,
        [Description("T")]
        Timestamps = 4,
        [Description("S")]
        SecurityNTFSACLs = 8,
        [Description("O")]
        OwnerInfo = 16,
        [Description("U")]
        AuditingInfo = 32
    }

    [Flags]
    public enum ChoCopyDirFlags
    {
        [Description("")]
        None = 0,
        [Description("D")]
        Data = 1,
        [Description("A")]
        Attributes = 2,
        [Description("T")]
        Timestamps = 4,
        [Description("E")]
        ExtendedAttributes = 8,
        [Description("X")]
        SkipAlternativeDataStreams = 16,
    }

    [Flags]
    public enum ChoFileAttributes
    {
        [Description("")]
        None = 0,
        [Description("R")]
        ReadOnly = 1,
        [Description("H")]
        Hidden = 2,
        [Description("A")]
        Archive = 4,
        [Description("S")]
        System = 8,
        [Description("C")]
        Compressed = 16,
        [Description("N")]
        NotContentIndexed = 32,
        [Description("E")]
        Encrypted = 64,
        [Description("T")]
        Temporary = 128
    }

    [Flags]
    public enum ChoFileSelectionAttributes
    {
        None = 0,
        [Description("R")]
        ReadOnly = 1,
        [Description("A")]
        Archive = 2,
        [Description("S")]
        System = 4,
        [Description("H")]
        Hidden = 8,
        [Description("C")]
        Compressed = 16,
        [Description("N")]
        NotContentIndexed = 32,
        [Description("E")]
        Encrypted = 64,
        [Description("T")]
        Temporary = 128,
        [Description("O")]
        Offline = 256
    }

    public enum ChoFileMoveAttributes
    {
        None,
        MoveFilesOnly,
        MoveDirectoriesAndFiles,
    }
    public abstract class ChoViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    [ChoNameValueConfigurationSection("applicationSettings" /*, BindingMode = ChoConfigurationBindingMode.OneWayToSource */, Silent = false)]
    public class ChoAppSettings : ChoViewModelBase, ICloneable<ChoAppSettings> //: ChoConfigurableObject
    {
        private readonly static XmlSerializer _xmlSerializer = new XmlSerializer(typeof(ChoAppSettings));
        private readonly static Dictionary<string, PropertyInfo> _propInfos = new Dictionary<string, PropertyInfo>();
        private readonly static Dictionary<PropertyInfo, object> _defaultValues = new Dictionary<PropertyInfo, object>();
        private readonly static ChoAppSettings DefaultInstance = new ChoAppSettings();

        #region Constructors

        static ChoAppSettings()
        {
            ChoPropertyInfoAttribute attr = null;
            foreach (var pi in ChoType.GetProperties(typeof(ChoAppSettings)))
            {
                attr = pi.GetCustomAttributes(false).OfType<ChoPropertyInfoAttribute>().FirstOrDefault();
                if (attr != null)
                {
                    _propInfos.Add(pi.Name, pi);
                    _defaultValues.Add(pi, attr.DefaultValue);
                }
            }

            ChoObject.ResetObject(DefaultInstance);
        }

        public ChoAppSettings()
        {
        }

        #endregion Constructors

        #region Instance Data Members (Others)

        private bool _showRoboCopyProgress;
        [Browsable(false)]
        [ChoPropertyInfo("ShowRoboCopyProgress")]
        //[XmlIgnore]
        public bool ShowRoboCopyProgress
        {
            get { return _showRoboCopyProgress; }
            set
            {
                if (value)
                {
                    //CopySubDirectories = false;
                    //CopyFlags = String.Empty;

                    //MirrorDirTree = true; //MIR = Mirror mode
                    NoProgress = true; //NP  = Don't show progress percentage in log
                    NoDirListLog = true; // NDL - No directory list log
                    NoFileClassLog = true; //NC  = Don't log file classes (existing, new file, etc.)
                    PrintByteSizes = true; //BYTES = Show file sizes in bytes
                    NoJobHeader = true; //NJH = Do not display robocopy job header (JH)
                    NoJobSummary = true; //NJS = Do not display robocopy job summary (JS)
                }

                _showRoboCopyProgress = value;
                NotifyPropertyChanged();
            }
        }

        private bool _showOutputLineNumbers;
        [Browsable(false)]
        [ChoPropertyInfo("showOutputLineNumbers")]
        public bool ShowOutputLineNumbers
        {
            get { return _showOutputLineNumbers; }
            set
            {
                _showOutputLineNumbers = value;
                NotifyPropertyChanged();
            }
        }

        private int _maxStatusMsgSize;
        [Browsable(false)]
        [ChoPropertyInfo("maxStatusMsgSize", DefaultValue = "1000")]
        public int MaxStatusMsgSize
        {
            get { return _maxStatusMsgSize; }
            set
            {
                if (value > 0)
                {
                    _maxStatusMsgSize = value;
                    NotifyPropertyChanged();
                }
            }
        }

        string _sourceDirectory;
        [Browsable(false)]
        [ChoPropertyInfo("sourceDirectory")]
        public string SourceDirectory
        {
            get { return _sourceDirectory; }
            set
            {
                _sourceDirectory = value;
                NotifyPropertyChanged();
            }
        }

        string _destDirectory;
        [Browsable(false)]
        [ChoPropertyInfo("destDirectory")]
        public string DestDirectory
        {
            get { return _destDirectory; }
            set
            {
                _destDirectory = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Others)

        #region Instance Data Members (Common Options)

        string _roboCopyFilePath;
        [Category("1. Common Options")]
        [Description("RoboCopy file path.")]
        [DisplayName("RoboCopyFilePath")]
        [ChoPropertyInfo("roboCopyFilePath", DefaultValue = "RoboCopy.exe")]
        public string RoboCopyFilePath
        {
            get { return _roboCopyFilePath; }
            set
            {
                _roboCopyFilePath = value;
                NotifyPropertyChanged();
            }
        }

        string _files;
        [Category("1. Common Options")]
        [Description("File(s) to copy (names/wildcards: default is \"*.*\").")]
        [DisplayName("Files")]
        [ChoPropertyInfo("files", DefaultValue = "*.*")]
        public string Files
        {
            get { return _files; }
            set
            {
                _files = value;
                NotifyPropertyChanged();
            }
        }

        string _additionalParams;
        [Category("1. Common Options")]
        [Description("Additional command line parameters (Optional).")]
        [DisplayName("AdditionalParams")]
        [ChoPropertyInfo("additionalParams", DefaultValue = "")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "BigTextEditor")]
        public string AdditionalParams
        {
            get { return _additionalParams; }
            set
            {
                _additionalParams = value;
                NotifyPropertyChanged();
            }
        }

        string _precommands;
        [Category("1. Common Options")]
        [Description("Specify MS-DOS commands to run before robocopy operations, separated by ; (Optional).")]
        [DisplayName("PreCommands")]
        [ChoPropertyInfo("precommands", DefaultValue = "")]
        //[Editor(typeof(ChoMultilineTextBoxEditor), typeof(ChoMultilineTextBoxEditor))]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "BigTextEditor")]
        public string Precommands
        {
            get { return _precommands; }
            set
            {
                _precommands = value;
                NotifyPropertyChanged();
            }
        }

        string _postcommands;
        [Category("1. Common Options")]
        [Description("Specify MS-DOS commands to run after robocopy operations, separated by ; (Optional).")]
        [DisplayName("Postcommands")]
        [ChoPropertyInfo("postcommands", DefaultValue = "")]
        //[Editor(typeof(ChoMultilineTextBoxEditor), typeof(ChoMultilineTextBoxEditor))]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "BigTextEditor")]
        public string Postcommands
        {
            get { return _postcommands; }
            set
            {
                _postcommands = value;
                NotifyPropertyChanged();
            }
        }

        string _comments;
        [Category("1. Common Options")]
        [Description("Short description of backup task.")]
        [DisplayName("Comments")]
        [ChoPropertyInfo("comments", DefaultValue = "")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "BigTextEditor")]
        public string Comments
        {
            get { return _comments; }
            set
            {
                _comments = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Common Options)

        #region Instance Data Members (Source Options)

        bool _copyNoEmptySubDirectories;
        [Category("2. Source Options")]
        [Description("Copy subdirectories, but not empty ones. (/S).")]
        [DisplayName("CopyNoEmptySubDirectories")]
        [ChoPropertyInfo("copyNoEmptySubDirectories")]
        public bool CopyNoEmptySubDirectories
        {
            get { return _copyNoEmptySubDirectories; }
            set
            {
                _copyNoEmptySubDirectories = value;
                NotifyPropertyChanged();
            }
        }

        bool _copySubDirectories;
        [Category("2. Source Options")]
        [Description("Copy subdirectories, including Empty ones. (/E).")]
        [DisplayName("CopySubDirectories")]
        [ChoPropertyInfo("copySubDirectories", DefaultValue = "true")]
        public bool CopySubDirectories
        {
            get { return _copySubDirectories; }
            set
            {
                _copySubDirectories = value;
                if (!value) ShowRoboCopyProgress = false;
                NotifyPropertyChanged();
            }
        }

        ChoCopyFileFlags _copyFileFlags;
        [Category("2. Source Options")]
        [Description("What to copy for files (default is /COPY:DAT). (copyflags : D=Data, A=Attributes, T=Timestamps, S=Security=NTFS ACLs, O=Owner info, U=aUditing info). (/COPY:flags).")]
        [DisplayName("CopyFlags")]
        [ChoPropertyInfo("copyFlags", DefaultValue = "Data,Attributes,Timestamps")]
        //[Editor(typeof(CopyFlagsEditor), typeof(CopyFlagsEditor))]
        [XmlIgnore]
        public ChoCopyFileFlags CopyFileFlags
        {
            get { return _copyFileFlags; }
            set
            {
                _copyFileFlags = value;
                NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        [XmlElement("CopyFileFlags")]
        public string CopyFileFlagsElement
        {
            get { return CopyFileFlags.ToString(); }
            set
            {
                if (value != null)
                {
                    if (!value.Contains(","))
                    value = value.Replace(" ", ",").Trim();
                }

                ChoCopyFileFlags copyFlags;
                if (Enum.TryParse<ChoCopyFileFlags>(value, out copyFlags))
                    _copyFileFlags = copyFlags;
            }
        }

        bool _copyFilesWithSecurity;
        [Category("2. Source Options")]
        [Description("Copy files with security (equivalent to /COPY:DATS). (/SEC).")]
        [DisplayName("CopyFilesWithSecurity")]
        [ChoPropertyInfo("copyFilesWithSecurity")]
        public bool CopyFilesWithSecurity
        {
            get { return _copyFilesWithSecurity; }
            set
            {
                _copyFilesWithSecurity = value;
                NotifyPropertyChanged();
            }
        }

        //bool _copyDirTimestamp;
        //[Category("2. Source Options")]
        //[Description("Copy Directory Timestamps. (/DCOPY:T).")]
        //[DisplayName("CopyDirTimestamp")]
        //[ChoPropertyInfo("copyDirTimestamp")]
        //public bool CopyDirTimestamp
        //{
        //    get { return _copyDirTimestamp; }
        //    set
        //    {
        //        _copyDirTimestamp = value;
        //        NotifyPropertyChanged();
        //    }
        //}


        ChoCopyDirFlags _copyDirFlags;
        [Category("2. Source Options")]
        [Description("What to copy in directories (default is /DCOPY:DA). (copyflags : D=Data, A=Attributes, T=Timestamps, X=Skip alt data streams). (/DCOPY:flags).")]
        [DisplayName("CopyDirFlags")]
        [ChoPropertyInfo("copyDirFlags", DefaultValue = "Data,Attributes")]
        //[Editor(typeof(CopyFlagsEditor), typeof(CopyFlagsEditor))]
        [XmlIgnore]
        public ChoCopyDirFlags CopyDirFlags
        {
            get { return _copyDirFlags; }
            set
            {
                _copyDirFlags = value;
                NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        [XmlElement("CopyDirFlags")]
        public string CopyDirFlagsElement
        {
            get { return CopyDirFlags.ToString(); }
            set
            {
                if (value != null)
                {
                    if (!value.Contains(","))
                        value = value.Replace(" ", ",").Trim();
                }

                ChoCopyDirFlags copyDirFlags;
                if (Enum.TryParse<ChoCopyDirFlags>(value, out copyDirFlags))
                    _copyDirFlags = copyDirFlags;
            }
        }


        bool _copyFilesWithFileInfo;
        [Category("2. Source Options")]
        [Description("Copy all file info (equivalent to /COPY:DATSOU). (/COPYALL).")]
        [DisplayName("CopyFilesWithFileInfo")]
        [ChoPropertyInfo("copyFilesWithFileInfo")]
        public bool CopyFilesWithFileInfo
        {
            get { return _copyFilesWithFileInfo; }
            set
            {
                _copyFilesWithFileInfo = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyFilesWithNoFileInfo;
        [Category("2. Source Options")]
        [Description("Copy no file info (useful with /PURGE). (/NOCOPY).")]
        [DisplayName("CopyFilesWithNoFileInfo")]
        [ChoPropertyInfo("copyFilesWithNoFileInfo")]
        public bool CopyFilesWithNoFileInfo
        {
            get { return _copyFilesWithNoFileInfo; }
            set
            {
                _copyFilesWithNoFileInfo = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyOnlyFilesWithArchiveAttributes;
        [Category("2. Source Options")]
        [Description("Copy only files with the Archive attribute set. (/A).")]
        [DisplayName("CopyOnlyFilesWithArchiveAttributes")]
        [ChoPropertyInfo("copyOnlyFilesWithArchiveAttributes")]
        public bool CopyOnlyFilesWithArchiveAttributes
        {
            get { return _copyOnlyFilesWithArchiveAttributes; }
            set
            {
                _copyOnlyFilesWithArchiveAttributes = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyOnlyFilesWithArchiveAttributesAndReset;
        [Category("2. Source Options")]
        [Description("Copy only files with the Archive attribute and reset it. (/M).")]
        [DisplayName("CopyOnlyFilesWithArchiveAttributesAndReset")]
        [ChoPropertyInfo("copyOnlyFilesWithArchiveAttributesAndReset")]
        public bool CopyOnlyFilesWithArchiveAttributesAndReset
        {
            get { return _copyOnlyFilesWithArchiveAttributesAndReset; }
            set
            {
                _copyOnlyFilesWithArchiveAttributesAndReset = value;
                NotifyPropertyChanged();
            }
        }

        uint _onlyCopyNLevels;
        [Category("2. Source Options")]
        [Description("Only copy the top n levels of the source directory tree. 0 - all levels. (/LEV:n).")]
        [DisplayName("OnlyCopyNLevels")]
        [ChoPropertyInfo("onlyCopyNLevels")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint OnlyCopyNLevels
        {
            get { return _onlyCopyNLevels; }
            set
            {
                _onlyCopyNLevels = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesOlderThanNDays;
        [Category("2. Source Options")]
        [Description("MAXimum file AGE - exclude files older than n days/date. (/MAXAGE:n).")]
        [DisplayName("ExcludeFilesOlderThanNDays")]
        [ChoPropertyInfo("excludeFilesOlderThanNDays")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint ExcludeFilesOlderThanNDays
        {
            get { return _excludeFilesOlderThanNDays; }
            set
            {
                _excludeFilesOlderThanNDays = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesNewerThanNDays;
        [Category("2. Source Options")]
        [Description("MINimum file AGE - exclude files newer than n days/date. (/MINAGE:n).")]
        [DisplayName("ExcludeFilesNewerThanNDays")]
        [ChoPropertyInfo("excludeFilesNewerThanNDays")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint ExcludeFilesNewerThanNDays
        {
            get { return _excludeFilesNewerThanNDays; }
            set
            {
                _excludeFilesNewerThanNDays = value;
                NotifyPropertyChanged();
            }
        }

        bool _assumeFATFileTimes;
        [Category("2. Source Options")]
        [Description("assume FAT File Times (2-second granularity). (/FFT).")]
        [DisplayName("AssumeFATFileTimes")]
        [ChoPropertyInfo("assumeFATFileTimes")]
        public bool AssumeFATFileTimes
        {
            get { return _assumeFATFileTimes; }
            set
            {
                _assumeFATFileTimes = value;
                NotifyPropertyChanged();
            }
        }

        bool _turnOffLongPath;
        [Category("2. Source Options")]
        [Description("Turn off very long path (> 256 characters) support. (/256).")]
        [DisplayName("TurnOffLongPath")]
        [ChoPropertyInfo("turnOffLongPath")]
        public bool TurnOffLongPath
        {
            get { return _turnOffLongPath; }
            set
            {
                _turnOffLongPath = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Source Options)

        #region Instance Data Members (Destination Options)

        ChoFileAttributes _addFileAttributes;
        [Category("3. Destination Options")]
        [Description("Add the given attributes to copied files. (/A+:[RASHCNET]).")]
        [DisplayName("AddFileAttributes")]
        [ChoPropertyInfo("addFileAttributes", DefaultValue = "")]
        //[Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        [XmlIgnore]
        public ChoFileAttributes AddFileAttributes
        {
            get { return _addFileAttributes; }
            set
            {
                _addFileAttributes = value;
                NotifyPropertyChanged();
            }
        }


        [Browsable(false)]
        [XmlElement("AddFileAttributes")]
        public string AddFileAttributesElement
        {
            get { return AddFileAttributes.ToString(); }
            set
            {
                if (value != null)
                {
                    if (!value.Contains(","))
                        value = value.Replace(" ", ",").Trim();
                }

                ChoFileAttributes addFileAttributes;
                if (Enum.TryParse<ChoFileAttributes>(value, out addFileAttributes))
                    _addFileAttributes = addFileAttributes;
            }
        }

        ChoFileAttributes _removeFileAttributes;
        [Category("3. Destination Options")]
        [Description("Remove the given Attributes from copied files. (/A-:[RASHCNET]).")]
        [DisplayName("RemoveFileAttributes")]
        [ChoPropertyInfo("removeFileAttributes", DefaultValue = "")]
        //[Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        [XmlIgnore]
        public ChoFileAttributes RemoveFileAttributes
        {
            get { return _removeFileAttributes; }
            set
            {
                _removeFileAttributes = value;
                NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        [XmlElement("RemoveFileAttributes")]
        public string RemoveFileAttributesElement
        {
            get { return RemoveFileAttributes.ToString(); }
            set
            {
                if (value != null)
                {
                    if (!value.Contains(","))
                        value = value.Replace(" ", ",").Trim();
                }

                ChoFileAttributes removeFileAttributes;
                if (Enum.TryParse<ChoFileAttributes>(value, out removeFileAttributes))
                    _removeFileAttributes = removeFileAttributes;
            }
        }

        bool _createFATFileNames;
        [Category("3. Destination Options")]
        [Description("Create destination files using 8.3 FAT file names only. (/FAT).")]
        [DisplayName("CreateFATFileNames")]
        [ChoPropertyInfo("createFATFileNames")]
        public bool CreateFATFileNames
        {
            get { return _createFATFileNames; }
            set
            {
                _createFATFileNames = value;
                NotifyPropertyChanged();
            }
        }

        bool _createDirTree;
        [Category("3. Destination Options")]
        [Description("Create directory tree and zero-length files only. (/CREATE).")]
        [DisplayName("CreateDirTree")]
        [ChoPropertyInfo("createDirTree")]
        public bool CreateDirTree
        {
            get { return _createDirTree; }
            set
            {
                _createDirTree = value;
                NotifyPropertyChanged();
            }
        }

        bool _compensateOneHourDSTTimeDiff;
        [Category("3. Destination Options")]
        [Description("Compensate for one-hour DST time differences. (/DST).")]
        [DisplayName("CompensateOneHourDSTTimeDiff")]
        [ChoPropertyInfo("compensateOneHourDSTTimeDiff")]
        public bool CompensateOneHourDSTTimeDiff
        {
            get { return _compensateOneHourDSTTimeDiff; }
            set
            {
                _compensateOneHourDSTTimeDiff = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Destination Options)

        #region Instance Data Members (Copy Options)

        bool _listOnly;
        [Category("4. Copy Options")]
        [Description("List only - don't copy, timestamp or delete any files. (/L).")]
        [DisplayName("ListOnly")]
        [ChoPropertyInfo("listOnly")]
        public bool ListOnly
        {
            get { return _listOnly; }
            set
            {
                _listOnly = value;
                NotifyPropertyChanged();
            }
        }

        ChoFileMoveAttributes _moveFilesAndDirectories;
        [Category("4. Copy Options")]
        [Description("Move files and dirs (delete from source after copying). (/MOV or /MOVE).")]
        [DisplayName("MoveFilesAndDirectories")]
        [ChoPropertyInfo("moveFilesAndDirectories", DefaultValue = "None")]
        //[Editor(typeof(FileMoveSelectionAttributesEditor), typeof(FileMoveSelectionAttributesEditor))]
        public ChoFileMoveAttributes MoveFilesAndDirectories
        {
            get { return _moveFilesAndDirectories; }
            set
            {
                _moveFilesAndDirectories = value;
                NotifyPropertyChanged();
            }
        }

        internal void SetMoveFilesAndDirectories(ChoFileMoveAttributes value)
        {
            _moveFilesAndDirectories = value;
            NotifyPropertyChanged(nameof(Comments));
        }


        bool _copySymbolicLinks;
        [Category("4. Copy Options")]
        [Description("Copy symbolic links versus the target. (/SL).")]
        [DisplayName("CopySymbolicLinks")]
        [ChoPropertyInfo("copySymbolicLinks")]
        public bool CopySymbolicLinks
        {
            get { return _copySymbolicLinks; }
            set
            {
                _copySymbolicLinks = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyFilesRestartableMode;
        [Category("4. Copy Options")]
        [Description("Copy files in restartable mode. (/Z).")]
        [DisplayName("CopyFilesRestartableMode")]
        [ChoPropertyInfo("copyFilesRestartableMode")]
        public bool CopyFilesRestartableMode
        {
            get { return _copyFilesRestartableMode; }
            set
            {
                _copyFilesRestartableMode = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyFilesBackupMode;
        [Category("4. Copy Options")]
        [Description("Copy files in Backup mode. (/B).")]
        [DisplayName("CopyFilesBackupMode")]
        [ChoPropertyInfo("copyFilesBackupMode")]
        public bool CopyFilesBackupMode
        {
            get { return _copyFilesBackupMode; }
            set
            {
                _copyFilesBackupMode = value;
                NotifyPropertyChanged();
            }
        }

        bool _unbufferredIOCopy;
        [Category("4. Copy Options")]
        [Description("Copy using unbuffered I/O (recommended for large files). (/J)")]
        [DisplayName("UnbufferredIOCopy")]
        [ChoPropertyInfo("unbufferredIOCopy")]
        public bool UnbufferredIOCopy
        {
            get { return _unbufferredIOCopy; }
            set
            {
                _unbufferredIOCopy = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyWithoutWindowsCopyOffload;
        [Category("4. Copy Options")]
        [Description("Copy files without using the Windows Copy Offload mechanism. (/NOOFFLOAD).")]
        [DisplayName("CopyWithoutWindowsCopyOffload")]
        [ChoPropertyInfo("copyWithoutWindowsCopyOffload")]
        public bool CopyWithoutWindowsCopyOffload
        {
            get { return _copyWithoutWindowsCopyOffload; }
            set
            {
                _copyWithoutWindowsCopyOffload = value;
                NotifyPropertyChanged();
            }
        }

        bool _encrptFileEFSRawMode;
        [Category("4. Copy Options")]
        [Description("Copy all encrypted files in EFS RAW mode. (/EFSRAW).")]
        [DisplayName("EncrptFileEFSRawMode")]
        [ChoPropertyInfo("encrptFileEFSRawMode")]
        public bool EncrptFileEFSRawMode
        {
            get { return _encrptFileEFSRawMode; }
            set
            {
                _encrptFileEFSRawMode = value;
                NotifyPropertyChanged();
            }
        }

        bool _fixFileTimeOnFiles;
        [Category("4. Copy Options")]
        [Description("Fix file times on all files, even skipped files. (/TIMFIX).")]
        [DisplayName("FixFileTimeOnFiles")]
        [ChoPropertyInfo("fixFileTimeOnFiles")]
        public bool FixFileTimeOnFiles
        {
            get { return _fixFileTimeOnFiles; }
            set
            {
                _fixFileTimeOnFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeOlderFiles;
        [Category("4. Copy Options")]
        [Description("eXclude Older files. (/XO).")]
        [DisplayName("ExcludeOlderFiles")]
        [ChoPropertyInfo("excludeOlderFiles")]
        public bool ExcludeOlderFiles
        {
            get { return _excludeOlderFiles; }
            set
            {
                _excludeOlderFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeChangedFiles;
        [Category("4. Copy Options")]
        [Description("eXclude Changed files. (/XC).")]
        [DisplayName("ExcludeChangedFiles")]
        [ChoPropertyInfo("excludeChangedFiles")]
        public bool ExcludeChangedFiles
        {
            get { return _excludeChangedFiles; }
            set
            {
                _excludeChangedFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeNewerFiles;
        [Category("4. Copy Options")]
        [Description("eXclude Newer files. (/XN).")]
        [DisplayName("ExcludeNewerFiles")]
        [ChoPropertyInfo("excludeNewerFiles")]
        public bool ExcludeNewerFiles
        {
            get { return _excludeNewerFiles; }
            set
            {
                _excludeNewerFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeExtraFilesAndDirs;
        [Category("4. Copy Options")]
        [Description("eXclude eXtra files and directories. (/XX).")]
        [DisplayName("ExcludeExtraFilesAndDirs")]
        [ChoPropertyInfo("excludeExtraFilesAndDirs")]
        public bool ExcludeExtraFilesAndDirs
        {
            get { return _excludeExtraFilesAndDirs; }
            set
            {
                _excludeExtraFilesAndDirs = value;
                NotifyPropertyChanged();
            }
        }

        string _excludeFilesWithGivenNames;
        [Category("4. Copy Options")]
        [Description("eXclude Files matching given names/paths/wildcards. Separate names with ;. (/XF).")]
        [DisplayName("ExcludeFilesWithGivenNames")]
        [ChoPropertyInfo("excludeFilesWithGivenNames", DefaultValue = "")]
        //[Editor(typeof(ChoPropertyGridFilePicker), typeof(ChoPropertyGridFilePicker))]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "BigTextEditor")]
        public string ExcludeFilesWithGivenNames
        {
            get { return _excludeFilesWithGivenNames; }
            set
            {
                _excludeFilesWithGivenNames = value;
                NotifyPropertyChanged();
            }
        }

        string _excludeDirsWithGivenNames;
        [Category("4. Copy Options")]
        [Description("eXclude Directories matching given names/paths. Separate names with ;. (/XD).")]
        [DisplayName("ExcludeDirsWithGivenNames")]
        [ChoPropertyInfo("excludeDirsWithGivenNames", DefaultValue = "")]
        //[Editor(typeof(ChoPropertyGridFolderPicker), typeof(ChoPropertyGridFolderPicker))]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "BigTextEditor")]
        public string ExcludeDirsWithGivenNames
        {
            get { return _excludeDirsWithGivenNames; }
            set
            {
                _excludeDirsWithGivenNames = value;
                NotifyPropertyChanged();
            }
        }

        ChoFileSelectionAttributes _includeFilesWithGivenAttributes;
        [Category("4. Copy Options")]
        [Description("Include only files with any of the given Attributes set. (/IA:[RASHCNETO]).")]
        [DisplayName("IncludeFilesWithGivenAttributes")]
        [ChoPropertyInfo("includeFilesWithGivenAttributes", DefaultValue = "")]
        //[Editor(typeof(FileSelectionAttributesEditor), typeof(FileSelectionAttributesEditor))]
        [XmlIgnore]
        public ChoFileSelectionAttributes IncludeFilesWithGivenAttributes
        {
            get { return _includeFilesWithGivenAttributes; }
            set
            {
                _includeFilesWithGivenAttributes = value;
                NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        [XmlElement("IncludeFilesWithGivenAttributes")]
        public string IncludeFilesWithGivenAttributesElement
        {
            get { return IncludeFilesWithGivenAttributes.ToString(); }
            set
            {
                if (value != null)
                {
                    if (!value.Contains(","))
                        value = value.Replace(" ", ",").Trim();
                }

                ChoFileSelectionAttributes includeFilesWithGivenAttributes;
                if (Enum.TryParse<ChoFileSelectionAttributes>(value, out includeFilesWithGivenAttributes))
                    _includeFilesWithGivenAttributes = includeFilesWithGivenAttributes;
            }
        }

        ChoFileSelectionAttributes _excludeFilesWithGivenAttributes;
        [Category("4. Copy Options")]
        [Description("eXclude files with any of the given Attributes set. (/XA:[RASHCNETO]).")]
        [DisplayName("ExcludeFilesWithGivenAttributes")]
        [ChoPropertyInfo("excludeFilesWithGivenAttributes", DefaultValue = "")]
        //[Editor(typeof(FileSelectionAttributesEditor), typeof(FileSelectionAttributesEditor))]
        [XmlIgnore]
        public ChoFileSelectionAttributes ExcludeFilesWithGivenAttributes
        {
            get { return _excludeFilesWithGivenAttributes; }
            set
            {
                _excludeFilesWithGivenAttributes = value;
                NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        [XmlElement("ExcludeFilesWithGivenAttributes")]
        public string ExcludeFilesWithGivenAttributesElement
        {
            get { return ExcludeFilesWithGivenAttributes.ToString(); }
            set
            {
                if (value != null)
                {
                    if (!value.Contains(","))
                        value = value.Replace(" ", ",").Trim();
                }

                ChoFileSelectionAttributes excludeFilesWithGivenAttributes;
                if (Enum.TryParse<ChoFileSelectionAttributes>(value, out excludeFilesWithGivenAttributes))
                    _excludeFilesWithGivenAttributes = excludeFilesWithGivenAttributes;
            }
        }

        bool _overrideModifiedFiles;
        [Category("4. Copy Options")]
        [Description("Include Modified files (differing change times). Otherwise the same. These files are not copied by default;. (/IM).")]
        [DisplayName("OverrideModifiedFiles")]
        [ChoPropertyInfo("overrideModifiedFiles")]
        public bool OverrideModifiedFiles
        {
            get { return _overrideModifiedFiles; }
            set
            {
                _overrideModifiedFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _includeSameFiles;
        [Category("4. Copy Options")]
        [Description("Include Same files. Overwrite files even if they are already the same. (/IS).")]
        [DisplayName("IncludeSameFiles")]
        [ChoPropertyInfo("includeSameFiles")]
        public bool IncludeSameFiles
        {
            get { return _includeSameFiles; }
            set
            {
                _includeSameFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _includeTweakedFiles;
        [Category("4. Copy Options")]
        [Description("Include Tweaked files. (/IT).")]
        [DisplayName("IncludeTweakedFiles")]
        [ChoPropertyInfo("includeTweakedFiles")]
        public bool IncludeTweakedFiles
        {
            get { return _includeTweakedFiles; }
            set
            {
                _includeTweakedFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeJunctionPoints;
        [Category("4. Copy Options")]
        [Description("eXclude Junction points and symbolic links. (normally included by default). (/XJ).")]
        [DisplayName("ExcludeJunctionPoints")]
        [ChoPropertyInfo("excludeJunctionPoints")]
        public bool ExcludeJunctionPoints
        {
            get { return _excludeJunctionPoints; }
            set
            {
                _excludeJunctionPoints = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeJunctionPointsForDirs;
        [Category("4. Copy Options")]
        [Description("eXclude Junction points and symbolic links for source directories. (/XJD).")]
        [DisplayName("ExcludeJunctionPointsForDirs")]
        [ChoPropertyInfo("excludeJunctionPointsForDirs")]
        public bool ExcludeJunctionPointsForDirs
        {
            get { return _excludeJunctionPointsForDirs; }
            set
            {
                _excludeJunctionPointsForDirs = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeJunctionPointsForFiles;
        [Category("4. Copy Options")]
        [Description("eXclude symbolic links for source files. (/XJF).")]
        [DisplayName("ExcludeJunctionPointsForFiles")]
        [ChoPropertyInfo("excludeJunctionPointsForFiles")]
        public bool ExcludeJunctionPointsForFiles
        {
            get { return _excludeJunctionPointsForFiles; }
            set
            {
                _excludeJunctionPointsForFiles = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesBiggerThanNBytes;
        [Category("4. Copy Options")]
        [Description("MAXimum file size - exclude files bigger than n bytes. (/MAX:n).")]
        [DisplayName("ExcludeFilesBiggerThanNBytes")]
        [ChoPropertyInfo("excludeFilesBiggerThanNBytes")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint ExcludeFilesBiggerThanNBytes
        {
            get { return _excludeFilesBiggerThanNBytes; }
            set
            {
                _excludeFilesBiggerThanNBytes = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesSmallerThanNBytes;
        [Category("4. Copy Options")]
        [Description("MINimum file size - exclude files smaller than n bytes. (/MIN:n).")]
        [DisplayName("ExcludeFilesSmallerThanNBytes")]
        [ChoPropertyInfo("excludeFilesSmallerThanNBytes")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint ExcludeFilesSmallerThanNBytes
        {
            get { return _excludeFilesSmallerThanNBytes; }
            set
            {
                _excludeFilesSmallerThanNBytes = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesUnusedSinceNDays;
        [Category("4. Copy Options")]
        [Description("MAXimum Last Access Date - exclude files unused since n. (/MAXLAD:n).")]
        [DisplayName("ExcludeFilesUnusedSinceNDays")]
        [ChoPropertyInfo("excludeFilesUnusedSinceNDays")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint ExcludeFilesUnusedSinceNDays
        {
            get { return _excludeFilesUnusedSinceNDays; }
            set
            {
                _excludeFilesUnusedSinceNDays = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesUsedSinceNDays;
        [Category("4. Copy Options")]
        [Description("MINimum Last Access Date - exclude files used since n. (If n < 1900 then n = n days, else n = YYYYMMDD date). (/MINLAD:n).")]
        [DisplayName("ExcludeFilesUsedSinceNDays")]
        [ChoPropertyInfo("excludeFilesUsedSinceNDays")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint ExcludeFilesUsedSinceNDays
        {
            get { return _excludeFilesUsedSinceNDays; }
            set
            {
                _excludeFilesUsedSinceNDays = value;
                NotifyPropertyChanged();
            }
        }

        bool _mirrorDirTree;
        [Category("4. Copy Options")]
        [Description("Mirror a directory tree (equivalent to /E plus /PURGE). (/MIR).")]
        [DisplayName("MirrorDirTree")]
        [ChoPropertyInfo("mirrorDirTree")]
        public bool MirrorDirTree
        {
            get { return _mirrorDirTree; }
            set
            {
                _mirrorDirTree = value;
                NotifyPropertyChanged();
            }
        }

        bool _delDestFileDirIfNotExistsInSource;
        [Category("4. Copy Options")]
        [Description("Delete dest files/dirs that no longer exist in source. (/PURGE).")]
        [DisplayName("DelDestFileDirIfNotExistsInSource")]
        [ChoPropertyInfo("delDestFileDirIfNotExistsInSource")]
        public bool DelDestFileDirIfNotExistsInSource
        {
            get { return _delDestFileDirIfNotExistsInSource; }
            set
            {
                _delDestFileDirIfNotExistsInSource = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeLonelyFilesAndDirs;
        [Category("4. Copy Options")]
        [Description("eXclude Lonely files and directories. (/XL).")]
        [DisplayName("ExcludeLonelyFilesAndDirs")]
        [ChoPropertyInfo("excludeLonelyFilesAndDirs")]
        public bool ExcludeLonelyFilesAndDirs
        {
            get { return _excludeLonelyFilesAndDirs; }
            set
            {
                _excludeLonelyFilesAndDirs = value;
                NotifyPropertyChanged();
            }
        }

        bool _fixFileSecurityOnFiles;
        [Category("4. Copy Options")]
        [Description("Fix file security on all files, even skipped files. (/SECFIX).")]
        [DisplayName("FixFileSecurityOnFiles")]
        [ChoPropertyInfo("fixFileSecurityOnFiles")]
        public bool FixFileSecurityOnFiles
        {
            get { return _fixFileSecurityOnFiles; }
            set
            {
                _fixFileSecurityOnFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _fallbackCopyFilesMode;
        [Category("4. Copy Options")]
        [Description("Use restartable mode; if access denied use Backup mode. (/ZB).")]
        [DisplayName("FallbackCopyFilesMode")]
        [ChoPropertyInfo("fallbackCopyFilesMode")]
        public bool FallbackCopyFilesMode
        {
            get { return _fallbackCopyFilesMode; }
            set
            {
                _fallbackCopyFilesMode = value;
                NotifyPropertyChanged();
            }
        }

        uint _interPacketGapInMS;
        [Category("4. Copy Options")]
        [Description("Inter-Packet Gap (ms), to free bandwidth on slow lines. (/IPG:n).")]
        [DisplayName("InterPacketGapInMS")]
        [ChoPropertyInfo("interPacketGapInMS")]
        public uint InterPacketGapInMS
        {
            get { return _interPacketGapInMS; }
            set
            {
                _interPacketGapInMS = value;
                NotifyPropertyChanged();
            }
        }

        private uint _multithreadCopy;
        [Category("4. Copy Options")]
        [Description("Do multi-threaded copies with n threads (default 8). n must be at least 1 and not greater than 128. This option is incompatible with the /IPG and /EFSRAW options. Redirect output using /LOG option for better performance. (/MT[:n]).")]
        [DisplayName("MultithreadCopy")]
        [ChoPropertyInfo("multithreadCopy", DefaultValue = "0")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint MultithreadCopy
        {
            get { return _multithreadCopy; }
            set
            {
                if (value < 1 || value > 128)
                    _multithreadCopy = 0;
                else
                    _multithreadCopy = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyNODirInfo;
        [Category("4. Copy Options")]
        [Description("COPY NO directory info (by default /DCOPY:DA is done). (/NODCOPY).")]
        [DisplayName("CopyNODirInfo")]
        [ChoPropertyInfo("copyNODirInfo")]
        public bool CopyNODirInfo
        {
            get { return _copyNODirInfo; }
            set
            {
                _copyNODirInfo = value;
                NotifyPropertyChanged();
            }
        }
        #endregion Instance Data Members (Copy Options)

        #region Instance Data Members (Monitoring Options)

        const string DefaultNoOfRetries = "1000000";

        uint _noOfRetries;
        [Category("5. Monitoring Options")]
        [Description("Number of Retries on failed copies: default 1 million. (/R:n).")]
        [DisplayName("NoOfRetries")]
        [ChoPropertyInfo("noOfRetries", DefaultValue = DefaultNoOfRetries)]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint NoOfRetries
        {
            get { return _noOfRetries; }
            set
            {
                _noOfRetries = value;
                NotifyPropertyChanged();
            }
        }

        const string DefaultWaitTimeBetweenRetries = "30";

        uint _waitTimeBetweenRetries;
        [Category("5. Monitoring Options")]
        [Description("Wait time between retries: default is 30 seconds. (/W:n).")]
        [DisplayName("WaitTimeBetweenRetries")]
        [ChoPropertyInfo("waitTimeBetweenRetries", DefaultValue = DefaultWaitTimeBetweenRetries)]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint WaitTimeBetweenRetries
        {
            get { return _waitTimeBetweenRetries; }
            set
            {
                _waitTimeBetweenRetries = value;
                NotifyPropertyChanged();
            }
        }

        bool _saveRetrySettingsToRegistry;
        [Category("5. Monitoring Options")]
        [Description("Save /R:n and /W:n in the Registry as default settings. (/REG).")]
        [DisplayName("SaveRetrySettingsToRegistry")]
        [ChoPropertyInfo("saveRetrySettingsToRegistry")]
        public bool SaveRetrySettingsToRegistry
        {
            get { return _saveRetrySettingsToRegistry; }
            set
            {
                _saveRetrySettingsToRegistry = value;
                NotifyPropertyChanged();
            }
        }

        bool _waitForSharenames;
        [Category("5. Monitoring Options")]
        [Description("Wait for sharenames to be defined (retry error 67). (/TBD).")]
        [DisplayName("WaitForSharenames")]
        [ChoPropertyInfo("waitForSharenames")]
        public bool WaitForSharenames
        {
            get { return _waitForSharenames; }
            set
            {
                _waitForSharenames = value;
                NotifyPropertyChanged();
            }
        }

        uint _runAgainWithNoChangesSeen;
        [Category("5. Monitoring Options")]
        [Description("Monitor source; run again when more than n changes seen. (/MON:n).")]
        [DisplayName("RunAgainWithNoChangesSeen")]
        [ChoPropertyInfo("runAgainWithNoChangesSeen")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint RunAgainWithNoChangesSeen
        {
            get { return _runAgainWithNoChangesSeen; }
            set
            {
                _runAgainWithNoChangesSeen = value;
                NotifyPropertyChanged();
            }
        }

        uint _runAgainWithChangesSeenInMin;
        [Category("5. Monitoring Options")]
        [Description("Monitor source; run again in m minutes time, if changed. (/MOT:m).")]
        [DisplayName("RunAgainWithChangesSeenInMin")]
        [ChoPropertyInfo("runAgainWithChangesSeenInMin")]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "NumericDropDownListEditor")]
        public uint RunAgainWithChangesSeenInMin
        {
            get { return _runAgainWithChangesSeenInMin; }
            set
            {
                _runAgainWithChangesSeenInMin = value;
                NotifyPropertyChanged();
            }
        }

        bool _checkRunHourPerFileBasis;
        [Category("5. Monitoring Options")]
        [Description("Check run hours on a Per File (not per pass) basis. (/PF).")]
        [DisplayName("CheckRunHourPerFileBasis")]
        [ChoPropertyInfo("checkRunHourPerFileBasis")]
        public bool CheckRunHourPerFileBasis
        {
            get { return _checkRunHourPerFileBasis; }
            set
            {
                _checkRunHourPerFileBasis = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Monitoring Options)

        #region Instance Data Members (Scheduling Options)

        private TimeSpan _runHourStartTime;
        [Category("6. Scheduling Options")]
        [Description("Run Hours StartTime, when new copies may be started after then. (/RH:hhmm-hhmm).")]
        [DisplayName("RunHourStartTime")]
        [ChoPropertyInfo("runHourStartTime")]
        [XmlIgnore]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "TimePickerEditor")]
        [IgnoreValue("__:__:__")]
        public TimeSpan RunHourStartTime
        {
            get { return _runHourStartTime; }
            set { _runHourStartTime = value; NotifyPropertyChanged(); }
        }

        [Browsable(false)]
        public long RunHourStartTimeTicks
        {
            get { return _runHourStartTime.Ticks; }
            set { _runHourStartTime = new TimeSpan(value); }
        }

        private TimeSpan _runHourEndTime;
        [Category("6. Scheduling Options")]
        [Description("Run Hours EndTime, when new copies may be Ended before then. (/RH:hhmm-hhmm).")]
        [DisplayName("RunHourEndTime")]
        [ChoPropertyInfo("runHourEndTime")]
        [XmlIgnore]
        [PropertyGridOptions(EditorDataTemplateResourceKey = "TimePickerEditor")]
        [IgnoreValue("__:__:__")]
        public TimeSpan RunHourEndTime
        {
            get { return _runHourEndTime; }
            set { _runHourEndTime = value; NotifyPropertyChanged(); }
        }

        [Browsable(false)]
        public long RunHourEndTimeTicks
        {
            get { return _runHourEndTime.Ticks; }
            set { _runHourEndTime = new TimeSpan(value); }
        }


        #endregion Instance Data Members (Scheduling Options)

        #region Instance Data Members (Logging Options)

        private bool _noProgress;
        [Category("7. Logging Options")]
        [Description("No Progress - don't display percentage copied. Suppresses the display of progress information. This can be useful when output is redirected to a file. (/NP).")]
        [DisplayName("NoProgress")]
        [ChoPropertyInfo("noProgress")]
        public bool NoProgress
        {
            get { return _noProgress; }
            set 
            { 
                _noProgress = value;
                if (!value) ShowRoboCopyProgress = false;
                NotifyPropertyChanged(); 
            }
        }

        private bool _unicode;
        [Category("7. Logging Options")]
        [Description("Display the status output as unicode text. (/unicode).")]
        [DisplayName("Unicode")]
        [ChoPropertyInfo("unicode")]
        public bool Unicode
        {
            get { return _unicode; }
            set { _unicode = value; NotifyPropertyChanged(); }
        }

        private string _outputLogFilePath;
        [Category("7. Logging Options")]
        [Description("Output status to LOG file (overwrite existing log). (/LOG:file).")]
        [DisplayName("OutputLogFilePath")]
        [ChoPropertyInfo("outputLogFilePath", DefaultValue = "")]
        public string OutputLogFilePath
        {
            get { return _outputLogFilePath; }
            set { _outputLogFilePath = value; NotifyPropertyChanged(); }
        }

        private string _unicodeOutputLogFilePath;
        [Category("7. Logging Options")]
        [Description("Output status to LOG file as UNICODE (overwrite existing log). (/UNILOG:file).")]
        [DisplayName("UnicodeOutputLogFilePath")]
        [ChoPropertyInfo("unicodeOutputLogFilePath", DefaultValue = "")]
        public string UnicodeOutputLogFilePath
        {
            get { return _unicodeOutputLogFilePath; }
            set { _unicodeOutputLogFilePath = value; NotifyPropertyChanged(); }
        }

        private string _appendOutputLogFilePath;
        [Category("7. Logging Options")]
        [Description("Output status to LOG file (append to existing log). (/LOG+:file).")]
        [DisplayName("AppendOutputLogFilePath")]
        [ChoPropertyInfo("appendOutputLogFilePath", DefaultValue = "")]
        public string AppendOutputLogFilePath
        {
            get { return _appendOutputLogFilePath; }
            set { _appendOutputLogFilePath = value; NotifyPropertyChanged(); }
        }

        private string _appendUnicodeOutputLogFilePath;
        [Category("7. Logging Options")]
        [Description("Output status to LOG file as UNICODE (append to existing log). (/UNILOG+:file).")]
        [DisplayName("AppendUnicodeOutputLogFilePath")]
        [ChoPropertyInfo("appendUnicodeOutputLogFilePath", DefaultValue = "")]
        public string AppendUnicodeOutputLogFilePath
        {
            get { return _appendUnicodeOutputLogFilePath; }
            set { _appendUnicodeOutputLogFilePath = value; NotifyPropertyChanged(); }
        }

        private bool _includeSourceFileTimestamp;
        [Category("7. Logging Options")]
        [Description("Include source file timestamps in the output. (/TS).")]
        [DisplayName("IncludeSourceFileTimestamp")]
        [ChoPropertyInfo("includeSourceFileTimestamp")]
        public bool IncludeSourceFileTimestamp
        {
            get { return _includeSourceFileTimestamp; }
            set { _includeSourceFileTimestamp = value; NotifyPropertyChanged(); }
        }

        private bool _includeFullPathName;
        [Category("7. Logging Options")]
        [Description("Include Full Pathname of files in the output. (/FP).")]
        [DisplayName("IncludeFullPathName")]
        [ChoPropertyInfo("includeFullPathName")]
        public bool IncludeFullPathName
        {
            get { return _includeFullPathName; }
            set { _includeFullPathName = value; NotifyPropertyChanged(); }
        }

        private bool _noFileSizeLog;
        [Category("7. Logging Options")]
        [Description("No Size - don't log file sizes. (/NS).")]
        [DisplayName("NoFileSizeLog")]
        [ChoPropertyInfo("noFileSizeLog")]
        public bool NoFileSizeLog
        {
            get { return _noFileSizeLog; }
            set { _noFileSizeLog = value; NotifyPropertyChanged(); }
        }

        private bool _noFileClassLog;
        [Category("7. Logging Options")]
        [Description("No Class - don't log file classes. (/NC).")]
        [DisplayName("NoFileClassLog")]
        [ChoPropertyInfo("noFileClassLog")]
        public bool NoFileClassLog
        {
            get { return _noFileClassLog; }
            set 
            { 
                _noFileClassLog = value;
                if (!value) ShowRoboCopyProgress = false;
                NotifyPropertyChanged(); 
            }
        }

        private bool _noFileNameLog;
        [Category("7. Logging Options")]
        [Description("No File List - don't log file names. Hides file names. Failures are still logged though. Any files files deleted or would be deleted if /L was omitted are always logged. (/NFL).")]
        [DisplayName("NoFileNameLog")]
        [ChoPropertyInfo("noFileNameLog")]
        public bool NoFileNameLog
        {
            get { return _noFileNameLog; }
            set { _noFileNameLog = value; NotifyPropertyChanged(); }
        }

        private bool _noDirListLog;
        [Category("7. Logging Options")]
        [Description("No Directory List - don't log directory names. Hides output of the directory listing. Full file pathnames are output to more easily track down problematic files. (/NDL).")]
        [DisplayName("NoDirListLog")]
        [ChoPropertyInfo("noDirListLog")]
        public bool NoDirListLog
        {
            get { return _noDirListLog; }
            set 
            { 
                _noDirListLog = value;
                if (!value) ShowRoboCopyProgress = false;
                NotifyPropertyChanged(); 
            }
        }

        //[Category("Logging Options")]
        //[Description("Output to console window, as well as the log file. (/TEE).")]
        //[DisplayName("NoDirListLog")]
        //[ChoPropertyInfo("noDirListLog")]
        //public bool NoDirListLog
        //{
        //    get;
        //    set;
        //}

        private bool _noJobHeader;
        [Category("7. Logging Options")]
        [Description("No Job Header. (/NJH).")]
        [DisplayName("NoJobHeader")]
        [ChoPropertyInfo("noJobHeader")]
        public bool NoJobHeader
        {
            get { return _noJobHeader; }
            set 
            { 
                _noJobHeader = value;
                if (!value) ShowRoboCopyProgress = false;
                NotifyPropertyChanged(); 
            }
        }

        private bool _noJobSummary;
        [Category("7. Logging Options")]
        [Description("No Job Summary. (/NJS).")]
        [DisplayName("NoJobSummary")]
        [ChoPropertyInfo("noJobSummary")]
        public bool NoJobSummary
        {
            get { return _noJobSummary; }
            set 
            { 
                _noJobSummary = value;
                if (!value) ShowRoboCopyProgress = false;
                NotifyPropertyChanged(); 
            }
        }

        private bool _printByteSizes;
        [Category("7. Logging Options")]
        [Description("Print sizes as bytes. (/BYTES).")]
        [DisplayName("PrintByteSizes")]
        [ChoPropertyInfo("printByteSizes")]
        public bool PrintByteSizes
        {
            get { return _printByteSizes; }
            set 
            { 
                _printByteSizes = value;
                if (!value) ShowRoboCopyProgress = false;
                NotifyPropertyChanged(); 
            }
        }

        private bool _reportExtraFiles;
        [Category("7. Logging Options")]
        [Description("Report all eXtra files, not just those selected. (/X).")]
        [DisplayName("ReportExtraFiles")]
        [ChoPropertyInfo("reportExtraFiles")]
        public bool ReportExtraFiles
        {
            get { return _reportExtraFiles; }
            set { _reportExtraFiles = value; NotifyPropertyChanged(); }
        }

        private bool _verboseOutput;
        [Category("7. Logging Options")]
        [Description("Produce Verbose output, showing skipped files. (/V).")]
        [DisplayName("VerboseOutput")]
        [ChoPropertyInfo("verboseOutput")]
        public bool VerboseOutput
        {
            get { return _verboseOutput; }
            set { _verboseOutput = value; NotifyPropertyChanged(); }
        }

        private bool _showEstTimeOfArrival;
        [Category("7. Logging Options")]
        [Description("Show Estimated Time of Arrival of copied files. (/ETA).")]
        [DisplayName("ShowEstTimeOfArrival")]
        [ChoPropertyInfo("showEstTimeOfArrival")]
        public bool ShowEstTimeOfArrival
        {
            get { return _showEstTimeOfArrival; }
            set { _showEstTimeOfArrival = value; NotifyPropertyChanged(); }
        }

        private bool _showDebugVolumeInfo;
        [Category("7. Logging Options")]
        [Description("Show debug volume information. (/DEBUG).")]
        [DisplayName("ShowDebugVolumeInfo")]
        [ChoPropertyInfo("showDebugVolumeInfo")]
        public bool ShowDebugVolumeInfo
        {
            get { return _showDebugVolumeInfo; }
            set { _showDebugVolumeInfo = value; NotifyPropertyChanged(); }
        }

        #endregion Instance Data Members (Logging Options)

        #region Commented

        //[Category("Copy Options")]
        //[Description("Move files (delete from source after copying). (/MOV).")]
        //[DisplayName("MoveFiles")]
        //[ChoPropertyInfo("moveFiles")]
        //public bool MoveFiles
        //{
        //    get;
        //    set;
        //}

        //[Category("Copy Options")]
        //[Description("Move files and dirs (delete from source after copying). (/MOVE).")]
        //[DisplayName("MoveFilesNDirs")]
        //[ChoPropertyInfo("moveFilesNDirs")]
        //public bool MoveFilesNDirs
        //{
        //    get;
        //    set;
        //}

        #endregion Commented

        public void Reset()
        {
            Copy(DefaultInstance, this);
            //ChoObject.ResetObject(this);
            //Persist();
            SourceDirectory = null;
            DestDirectory = null;
            MultithreadCopy = 0;
            Precommands = null;
            Postcommands = null;
            Comments = null;
        }
        
        public string GetCmdLineText()
        {
            return "{0} {1}".FormatString(RoboCopyFilePath, GetCmdLineParams());
        }

        public string GetCmdLineTextEx()
        {
            return "{0} {1} {2} {3}".FormatString(RoboCopyFilePath, GetCmdLineParams(), GetExCmdLineParams(), Comments);
        }

        string DirSafeguard(string path)
        {
            // Escape the last '\' from the path if it is not escaped yet.
            if (path.Length > 1 && path.Last() == '\\' && (path[path.Length - 2] != '\\'))
                path += '\\';
            return path;
        }

        internal string GetExCmdLineParams()
        {
            StringBuilder cmdText = new StringBuilder();

            if (!Postcommands.IsNullOrWhiteSpace())
                cmdText.Append(Postcommands);
            if (!Precommands.IsNullOrWhiteSpace())
                cmdText.Append(Precommands);
            cmdText.Append($"/ShowOutputLineNumbers:{ShowOutputLineNumbers}");
            cmdText.Append($"/ShowRoboCopyProgress:{ShowRoboCopyProgress}");

            return cmdText.ToString();
        }
        internal string GetCmdLineParams(string sourceDirectory = null, string destDirectory = null)
        {
            StringBuilder cmdText = new StringBuilder();
            
            if (!sourceDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", DirSafeguard(sourceDirectory));
            else if (!SourceDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", DirSafeguard(SourceDirectory));

            if (!destDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", DirSafeguard(destDirectory));
            else if (!DestDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", DirSafeguard(DestDirectory));

            if (!Files.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" {0}", Files);
            else
                cmdText.Append("*.*");

            //Copy Options
            if (CopyNoEmptySubDirectories)
                cmdText.Append(" /S");
            if (CopySubDirectories)
                cmdText.Append(" /E");
            if (OnlyCopyNLevels > 0)
                cmdText.AppendFormat(" /LEV:{0}", OnlyCopyNLevels);
            if (CopyFilesRestartableMode)
                cmdText.Append(" /Z");
            if (CopyFilesBackupMode)
                cmdText.Append(" /B");
            if (FallbackCopyFilesMode)
                cmdText.Append(" /ZB");
            if (UnbufferredIOCopy)
                cmdText.Append(" /J");

            if (EncrptFileEFSRawMode)
                cmdText.Append(" /EFSRAW");

            StringBuilder copyFileFlags = new StringBuilder();
            if ((CopyFileFlags & ChoCopyFileFlags.Data) == ChoCopyFileFlags.Data)
                copyFileFlags.Append($"D");
            if ((CopyFileFlags & ChoCopyFileFlags.Attributes) == ChoCopyFileFlags.Attributes)
                copyFileFlags.Append($"A");
            if ((CopyFileFlags & ChoCopyFileFlags.Timestamps) == ChoCopyFileFlags.Timestamps)
                copyFileFlags.Append($"T");
            if ((CopyFileFlags & ChoCopyFileFlags.SecurityNTFSACLs) == ChoCopyFileFlags.SecurityNTFSACLs)
                copyFileFlags.Append($"S");
            if ((CopyFileFlags & ChoCopyFileFlags.OwnerInfo) == ChoCopyFileFlags.OwnerInfo)
                copyFileFlags.Append($"O");
            if ((CopyFileFlags & ChoCopyFileFlags.AuditingInfo) == ChoCopyFileFlags.AuditingInfo)
                copyFileFlags.Append($"U");
            if (copyFileFlags.Length > 0)
                cmdText.AppendFormat(" /COPY:{0}", copyFileFlags.ToString());

            StringBuilder copyDirFlags = new StringBuilder();
            if ((CopyDirFlags & ChoCopyDirFlags.Data) == ChoCopyDirFlags.Data)
                copyDirFlags.Append($"D");
            if ((CopyDirFlags & ChoCopyDirFlags.Attributes) == ChoCopyDirFlags.Attributes)
                copyDirFlags.Append($"A");
            if ((CopyDirFlags & ChoCopyDirFlags.Timestamps) == ChoCopyDirFlags.Timestamps)
                copyDirFlags.Append($"T");
            if ((CopyDirFlags & ChoCopyDirFlags.ExtendedAttributes) == ChoCopyDirFlags.ExtendedAttributes)
                copyDirFlags.Append($"E");
            if ((CopyDirFlags & ChoCopyDirFlags.SkipAlternativeDataStreams) == ChoCopyDirFlags.SkipAlternativeDataStreams)
                copyDirFlags.Append($"X");
            if (copyDirFlags.Length > 0)
                cmdText.AppendFormat(" /DCOPY:{0}", copyDirFlags.ToString());

            if (CopyFilesWithSecurity)
                cmdText.Append(" /SEC");

            if (CopyFilesWithFileInfo)
                cmdText.Append(" /COPYALL");
            if (CopyFilesWithNoFileInfo)
                cmdText.Append(" /NOCOPY");
            if (FixFileSecurityOnFiles)
                cmdText.Append(" /SECFIX");
            if (FixFileTimeOnFiles)
                cmdText.Append(" /TIMFIX");

            if (DelDestFileDirIfNotExistsInSource)
                cmdText.Append(" /PURGE");
            if (MirrorDirTree)
                cmdText.Append(" /MIR");
            //if (MoveFiles)
            //    cmdText.Append(" /MOV");
            //if (MoveFilesNDirs)
            //    cmdText.Append(" /MOVE");
            switch (MoveFilesAndDirectories)
            {
                case ChoFileMoveAttributes.MoveFilesOnly:
                    cmdText.Append(" /MOV");
                    break;
                case ChoFileMoveAttributes.MoveDirectoriesAndFiles:
                    cmdText.Append(" /MOVE");
                    break;
                default:
                    break;
            }

            StringBuilder addFileAttributes = new StringBuilder();
            if ((AddFileAttributes & ChoFileAttributes.ReadOnly) == ChoFileAttributes.ReadOnly)
                addFileAttributes.Append($"R");
            if ((AddFileAttributes & ChoFileAttributes.Hidden) == ChoFileAttributes.Hidden)
                addFileAttributes.Append($"H");
            if ((AddFileAttributes & ChoFileAttributes.Archive) == ChoFileAttributes.Archive)
                addFileAttributes.Append($"A");
            if ((AddFileAttributes & ChoFileAttributes.System) == ChoFileAttributes.System)
                addFileAttributes.Append($"S");
            if ((AddFileAttributes & ChoFileAttributes.Compressed) == ChoFileAttributes.Compressed)
                addFileAttributes.Append($"C");
            if ((AddFileAttributes & ChoFileAttributes.NotContentIndexed) == ChoFileAttributes.NotContentIndexed)
                addFileAttributes.Append($"N");
            if ((AddFileAttributes & ChoFileAttributes.Encrypted) == ChoFileAttributes.Encrypted)
                addFileAttributes.Append($"E");
            if ((AddFileAttributes & ChoFileAttributes.Temporary) == ChoFileAttributes.Temporary)
                addFileAttributes.Append($"T");
            if (addFileAttributes.Length > 0)
                cmdText.AppendFormat(" /A+:{0}", addFileAttributes.ToString());

            StringBuilder removeFileAttributes = new StringBuilder();
            if ((RemoveFileAttributes & ChoFileAttributes.ReadOnly) == ChoFileAttributes.ReadOnly)
                removeFileAttributes.Append($"R");
            if ((RemoveFileAttributes & ChoFileAttributes.Hidden) == ChoFileAttributes.Hidden)
                removeFileAttributes.Append($"H");
            if ((RemoveFileAttributes & ChoFileAttributes.Archive) == ChoFileAttributes.Archive)
                removeFileAttributes.Append($"A");
            if ((RemoveFileAttributes & ChoFileAttributes.System) == ChoFileAttributes.System)
                removeFileAttributes.Append($"S");
            if ((RemoveFileAttributes & ChoFileAttributes.Compressed) == ChoFileAttributes.Compressed)
                removeFileAttributes.Append($"C");
            if ((RemoveFileAttributes & ChoFileAttributes.NotContentIndexed) == ChoFileAttributes.NotContentIndexed)
                removeFileAttributes.Append($"N");
            if ((RemoveFileAttributes & ChoFileAttributes.Encrypted) == ChoFileAttributes.Encrypted)
                removeFileAttributes.Append($"E");
            if ((RemoveFileAttributes & ChoFileAttributes.Temporary) == ChoFileAttributes.Temporary)
                removeFileAttributes.Append($"T");
            if (removeFileAttributes.Length > 0)
                cmdText.AppendFormat(" /A-:{0}", removeFileAttributes.ToString());

            if (CreateDirTree)
                cmdText.Append(" /CREATE");
            if (CreateFATFileNames)
                cmdText.Append(" /FAT");
            if (TurnOffLongPath)
                cmdText.Append(" /256");

            if (RunAgainWithNoChangesSeen > 0)
                cmdText.AppendFormat(" /MON:{0}", RunAgainWithNoChangesSeen);
            if (RunAgainWithChangesSeenInMin > 0)
                cmdText.AppendFormat(" /MOT:{0}", RunAgainWithChangesSeenInMin);
            if (RunHourStartTime != TimeSpan.Zero
                && RunHourEndTime != TimeSpan.Zero
                && RunHourStartTime < RunHourEndTime)
                cmdText.AppendFormat(" /RH:{0}-{1}", RunHourStartTime.ToString("hhmm"), RunHourEndTime.ToString("hhmm"));
            if (CheckRunHourPerFileBasis)
                cmdText.Append(" /PF");
            if (InterPacketGapInMS > 0)
                cmdText.AppendFormat(" /IPG:{0}", InterPacketGapInMS);
            if (CopySymbolicLinks)
                cmdText.Append(" /SL");
            if (MultithreadCopy > 0)
                cmdText.AppendFormat(" /MT:{0}", MultithreadCopy);
            if (CopyNODirInfo)
                cmdText.Append(" /NODCOPY");
            if (CopyWithoutWindowsCopyOffload)
                cmdText.Append(" /NOOFFLOAD");
            if (OverrideModifiedFiles)
                cmdText.Append(" /IM");
            //File Selection Options
            if (CopyOnlyFilesWithArchiveAttributes)
                cmdText.Append(" /A");
            if (CopyOnlyFilesWithArchiveAttributesAndReset)
                cmdText.Append(" /M");

            StringBuilder includeFilesWithGivenAttributes = new StringBuilder();
            if ((IncludeFilesWithGivenAttributes & ChoFileSelectionAttributes.ReadOnly) == ChoFileSelectionAttributes.ReadOnly)
                removeFileAttributes.Append($"R");
            if ((IncludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Archive) == ChoFileSelectionAttributes.Archive)
                removeFileAttributes.Append($"A");
            if ((IncludeFilesWithGivenAttributes & ChoFileSelectionAttributes.System) == ChoFileSelectionAttributes.System)
                removeFileAttributes.Append($"S");
            if ((IncludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Hidden) == ChoFileSelectionAttributes.Hidden)
                removeFileAttributes.Append($"H");
            if ((IncludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Compressed) == ChoFileSelectionAttributes.Compressed)
                removeFileAttributes.Append($"C");
            if ((IncludeFilesWithGivenAttributes & ChoFileSelectionAttributes.NotContentIndexed) == ChoFileSelectionAttributes.NotContentIndexed)
                removeFileAttributes.Append($"N");
            if ((IncludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Encrypted) == ChoFileSelectionAttributes.Encrypted)
                removeFileAttributes.Append($"E");
            if ((IncludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Temporary) == ChoFileSelectionAttributes.Temporary)
                removeFileAttributes.Append($"T");
            if ((IncludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Offline) == ChoFileSelectionAttributes.Offline)
                removeFileAttributes.Append($"O");
            if (includeFilesWithGivenAttributes.Length > 0)
                cmdText.AppendFormat(" /IA:{0}", includeFilesWithGivenAttributes.ToString());


            StringBuilder excludeFilesWithGivenAttributes = new StringBuilder();
            if ((ExcludeFilesWithGivenAttributes & ChoFileSelectionAttributes.ReadOnly) == ChoFileSelectionAttributes.ReadOnly)
                removeFileAttributes.Append($"R");
            if ((ExcludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Archive) == ChoFileSelectionAttributes.Archive)
                removeFileAttributes.Append($"A");
            if ((ExcludeFilesWithGivenAttributes & ChoFileSelectionAttributes.System) == ChoFileSelectionAttributes.System)
                removeFileAttributes.Append($"S");
            if ((ExcludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Hidden) == ChoFileSelectionAttributes.Hidden)
                removeFileAttributes.Append($"H");
            if ((ExcludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Compressed) == ChoFileSelectionAttributes.Compressed)
                removeFileAttributes.Append($"C");
            if ((ExcludeFilesWithGivenAttributes & ChoFileSelectionAttributes.NotContentIndexed) == ChoFileSelectionAttributes.NotContentIndexed)
                removeFileAttributes.Append($"N");
            if ((ExcludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Encrypted) == ChoFileSelectionAttributes.Encrypted)
                removeFileAttributes.Append($"E");
            if ((ExcludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Temporary) == ChoFileSelectionAttributes.Temporary)
                removeFileAttributes.Append($"T");
            if ((ExcludeFilesWithGivenAttributes & ChoFileSelectionAttributes.Offline) == ChoFileSelectionAttributes.Offline)
                removeFileAttributes.Append($"O");
            if (excludeFilesWithGivenAttributes.Length > 0)
                cmdText.AppendFormat(" /XA:{0}", excludeFilesWithGivenAttributes.ToString());
            
            if (!ExcludeFilesWithGivenNames.IsNullOrWhiteSpace())
                cmdText.AppendFormat(@" /XF {0}", String.Join(" ", ExcludeFilesWithGivenNames.Split(";").Select(f => f).Select(f => f.Contains(" ") ? String.Format(@"""{0}""", f) : f)));
            if (!ExcludeDirsWithGivenNames.IsNullOrWhiteSpace())
                cmdText.AppendFormat(@" /XD {0}", String.Join(" ", ExcludeDirsWithGivenNames.Split(";").Select(f => f).Select(f => f.Contains(" ") ? String.Format(@"""{0}""", f) : f)));
            if (ExcludeChangedFiles)
                cmdText.Append(" /XC");
            if (ExcludeNewerFiles)
                cmdText.Append(" /XN");
            if (ExcludeOlderFiles)
                cmdText.Append(" /XO");
            if (ExcludeExtraFilesAndDirs)
                cmdText.Append(" /XX");
            if (ExcludeLonelyFilesAndDirs)
                cmdText.Append(" /XL");
            if (IncludeSameFiles)
                cmdText.Append(" /IS");
            if (IncludeTweakedFiles)
                cmdText.Append(" /IT");

            if (ExcludeFilesBiggerThanNBytes > 0)
                cmdText.AppendFormat(" /MAX:{0}", ExcludeFilesBiggerThanNBytes);
            if (ExcludeFilesSmallerThanNBytes > 0)
                cmdText.AppendFormat(" /MIN:{0}", ExcludeFilesSmallerThanNBytes);

            if (ExcludeFilesOlderThanNDays > 0)
                cmdText.AppendFormat(" /MAXAGE:{0}", ExcludeFilesOlderThanNDays);
            if (ExcludeFilesNewerThanNDays > 0)
                cmdText.AppendFormat(" /MINAGE:{0}", ExcludeFilesNewerThanNDays);
            if (ExcludeFilesUnusedSinceNDays > 0)
                cmdText.AppendFormat(" /MAXLAD:{0}", ExcludeFilesUnusedSinceNDays);
            if (ExcludeFilesUsedSinceNDays > 0)
                cmdText.AppendFormat(" /MINLAD:{0}", ExcludeFilesUsedSinceNDays);

            if (ExcludeJunctionPoints)
                cmdText.Append(" /XJ");
            if (AssumeFATFileTimes)
                cmdText.Append(" /FFT");
            if (CompensateOneHourDSTTimeDiff)
                cmdText.Append(" /DST");
            if (ExcludeJunctionPointsForDirs)
                cmdText.Append(" /XJD");
            if (ExcludeJunctionPointsForFiles)
                cmdText.Append(" /XJF");

            //Retry Options
            if (NoOfRetries.ToString() != DefaultNoOfRetries && NoOfRetries >= 0)
                cmdText.AppendFormat(" /R:{0}", NoOfRetries);
            if (WaitTimeBetweenRetries.ToString() != DefaultWaitTimeBetweenRetries && WaitTimeBetweenRetries >= 0)
                cmdText.AppendFormat(" /W:{0}", WaitTimeBetweenRetries);
            if (SaveRetrySettingsToRegistry)
                cmdText.Append(" /REG");
            if (WaitForSharenames)
                cmdText.Append(" /TBD");

            //Logging Options
            if (ListOnly)
                cmdText.Append(" /L");
            if (ReportExtraFiles)
                cmdText.Append(" /X");
            if (VerboseOutput)
                cmdText.Append(" /V");
            if (IncludeSourceFileTimestamp)
                cmdText.Append(" /TS");
            if (IncludeFullPathName)
                cmdText.Append(" /FP");
            if (PrintByteSizes)
                cmdText.Append(" /BYTES");
            if (NoFileSizeLog)
                cmdText.Append(" /NS");
            if (NoFileClassLog)
                cmdText.Append(" /NC");
            if (NoFileNameLog)
                cmdText.Append(" /NFL");
            if (NoDirListLog)
                cmdText.Append(" /NDL");
            if (NoProgress)
                cmdText.Append(" /NP");
            if (Unicode)
                cmdText.Append(" /unicode");
            if (ShowEstTimeOfArrival)
                cmdText.Append(" /ETA");
            if (ShowDebugVolumeInfo)
                cmdText.Append(" /DEBUG");
            if (!OutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /LOG:\"{0}\"", OutputLogFilePath);
            if (!AppendOutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /LOG+:\"{0}\"", AppendOutputLogFilePath);
            if (!UnicodeOutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /UNILOG:\"{0}\"", UnicodeOutputLogFilePath);
            if (!AppendUnicodeOutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /UNILOG+:\"{0}\"", AppendUnicodeOutputLogFilePath);
            if (NoJobHeader)
                cmdText.Append(" /NJH");
            if (NoJobSummary)
                cmdText.Append(" /NJS");

            if (!AdditionalParams.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" {0}", AdditionalParams);

            return cmdText.ToString();
        }

        //TODO
        //protected override void OnAfterConfigurationObjectLoaded()
        //{
        //    if (RoboCopyFilePath.IsNullOrWhiteSpace())
        //        RoboCopyFilePath = "RoboCopy.exe";
        //}

        public void Persist()
        {

        }

        //public string GetXml()
        //{
        //    StringBuilder xml = new StringBuilder();
        //    using (StringWriter w = new StringWriter(xml))
        //    {
        //        _xmlSerializer.Serialize(w, this);
        //        return xml.ToString();
        //    }
        //}

        public void LoadXml(string xml)
        {
            using (StringReader reader = new StringReader(xml))
            {
                var appSettings = _xmlSerializer.Deserialize(reader) as ChoAppSettings;
                Copy(appSettings, this);
            }
        }

        public static void Copy(ChoAppSettings src, ChoAppSettings dest)
        {
            if (src == null || dest == null)
                return;

            foreach (var pi in _propInfos.Values)
            {
                SetPropertyValue(dest, pi, ChoType.GetPropertyValue(src, pi));
            }
        }
        private static readonly Dictionary<IntPtr, Action<object, object>> _setterCache = new Dictionary<IntPtr, Action<object, object>>();
        private static readonly object _padLock = new object();
        public static void SetPropertyValue(object target, PropertyInfo propertyInfo, object val)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNull(propertyInfo, "PropertyInfo");

            if ((val == null || (val is string && ((string)val).IsEmpty())) && propertyInfo.PropertyType.IsValueType)
            {
                if (propertyInfo.PropertyType.IsNullableType())
                    val = null;
                else
                    val = propertyInfo.PropertyType.Default();
            }

            try
            {
                Action<object, object> setter;
                var mi = propertyInfo.GetSetMethod();
                if (mi != null)
                {
                    var key = mi.MethodHandle.Value;
                    if (!_setterCache.TryGetValue(key, out setter))
                    {
                        lock (_padLock)
                        {
                            if (!_setterCache.TryGetValue(key, out setter))
                                _setterCache.Add(key, setter = propertyInfo.CreateSetMethod());
                        }
                    }

                    setter(target, val);
                }
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, propertyInfo.Name), ex.InnerException);
            }
        }

        public ChoAppSettings Clone()
        {
            var obj = new ChoAppSettings();
            Copy(this, obj);
            return obj;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
