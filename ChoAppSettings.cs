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
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using System.Windows;
    using Cinchoo.Core.WPF;
    using System.Windows.Data;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Windows.Controls;
    using System.Windows.Threading;

    #endregion NameSpaces

    [Flags]
    public enum ChoCopyFlags
    {
        [Description("D")]
        Data,
        [Description("A")]
        Attributes,
        [Description("T")]
        Timestamps,
        [Description("S")]
        SecurityNTFSACLs,
        [Description("O")]
        OwnerInfo,
        [Description("U")]
        AuditingInfo
    }

    public enum ChoFileAttributes
    {
        [Description("R")]
        ReadOnly,
        [Description("H")]
        Hidden,
        [Description("A")]
        Archive,
        [Description("S")]
        System,
        [Description("C")]
        Compressed,
        [Description("N")]
        NotContentIndexed,
        [Description("E")]
        Encrypted,
        [Description("T")]
        Temporary
    }

    public enum ChoFileSelectionAttributes
    {
        [Description("R")]
        ReadOnly,
        [Description("A")]
        Archive,
        [Description("S")]
        System,
        [Description("H")]
        Hidden,
        [Description("C")]
        Compressed,
        [Description("N")]
        NotContentIndexed,
        [Description("E")]
        Encrypted,
        [Description("T")]
        Temporary,
        [Description("O")]
        Offline
    }

    public enum ChoFileMoveAttributes
    {
        [Description("")]
        None,
        [Description("/MOV")]
        MoveFilesOnly,
        [Description("/MOVE")]
        MoveDirectoriesAndFiles,
    }

    [ChoNameValueConfigurationSection("applicationSettings" /*, BindingMode = ChoConfigurationBindingMode.OneWayToSource */, Silent = false)]
    public class ChoAppSettings : ChoConfigurableObject
    {
        #region Instance Data Members (Others)

        [Browsable(false)]
        [ChoPropertyInfo("showOutputLineNumbers")]
        public bool ShowOutputLineNumbers
        {
            get;
            set;
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
                    _maxStatusMsgSize = value;
            }
        }

        [Browsable(false)]
        [ChoPropertyInfo("sourceDirectory")]
        public string SourceDirectory
        {
            get;
            set;
        }

        [Browsable(false)]
        [ChoPropertyInfo("destDirectory")]
        public string DestDirectory
        {
            get;
            set;
        }

        #endregion Instance Data Members (Others)

        #region Instance Data Members (Common Options)

        [Category("1. Common Options")]
        [Description("RoboCopy file path.")]
        [DisplayName("RoboCopyFilePath")]
        [ChoPropertyInfo("roboCopyFilePath", DefaultValue = "RoboCopy.exe")]
        public string RoboCopyFilePath
        {
            get;
            set;
        }

        [Category("1. Common Options")]
        [Description("File(s) to copy (names/wildcards: default is \"*.*\").")]
        [DisplayName("Files")]
        [ChoPropertyInfo("files", DefaultValue = "*.*")]
        public string Files
        {
            get;
            set;
        }

        [Category("1. Common Options")]
        [Description("Additional command line parameters (Optional).")]
        [DisplayName("AdditionalParams")]
        [ChoPropertyInfo("additionalParams", DefaultValue = "")]
        public string AdditionalParams
        {
            get;
            set;
        }

        [Category("1. Common Options")]
        [Description("Specify MS-DOS commands to run before robocopy operations, separated by ; (Optional).")]
        [DisplayName("PreCommands")]
        [ChoPropertyInfo("precommands", DefaultValue = "")]
        public string Precommands
        {
            get;
            set;
        }

        [Category("1. Common Options")]
        [Description("Specify MS-DOS commands to run after robocopy operations, separated by ; (Optional).")]
        [DisplayName("Postcommands")]
        [ChoPropertyInfo("postcommands", DefaultValue = "")]
        public string Postcommands
        {
            get;
            set;
        }

        [Category("1. Common Options")]
        [Description("Short description of backup task.")]
        [DisplayName("Comments")]
        [ChoPropertyInfo("comments", DefaultValue = "")]
        public string Comments
        {
            get;
            set;
        }

        #endregion Instance Data Members (Common Options)

        #region Instance Data Members (Source Options)

        [Category("2. Source Options")]
        [Description("Copy subdirectories, but not empty ones. (/S).")]
        [DisplayName("CopyNoEmptySubDirectories")]
        [ChoPropertyInfo("copyNoEmptySubDirectories")]
        public bool CopyNoEmptySubDirectories
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("Copy subdirectories, including Empty ones. (/E).")]
        [DisplayName("CopySubDirectories")]
        [ChoPropertyInfo("copySubDirectories", DefaultValue = "true")]
        public bool CopySubDirectories
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("What to copy for files (default is /COPY:DAT). (copyflags : D=Data, A=Attributes, T=Timestamps, S=Security=NTFS ACLs, O=Owner info, U=aUditing info). (/COPY:flags).")]
        [DisplayName("CopyFlags")]
        [ChoPropertyInfo("copyFlags", DefaultValue = "Data,Attributes,Timestamps")]
        [Editor(typeof(CopyFlagsEditor), typeof(CopyFlagsEditor))]
        public string CopyFlags
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("Copy files with security (equivalent to /COPY:DATS). (/SEC).")]
        [DisplayName("CopyFilesWithSecurity")]
        [ChoPropertyInfo("copyFilesWithSecurity")]
        public bool CopyFilesWithSecurity
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("Copy Directory Timestamps. (/DCOPY:T).")]
        [DisplayName("CopyDirTimestamp")]
        [ChoPropertyInfo("copyDirTimestamp")]
        public bool CopyDirTimestamp
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("Copy all file info (equivalent to /COPY:DATSOU). (/COPYALL).")]
        [DisplayName("CopyFilesWithFileInfo")]
        [ChoPropertyInfo("copyFilesWithFileInfo")]
        public bool CopyFilesWithFileInfo
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("Copy no file info (useful with /PURGE). (/NOCOPY).")]
        [DisplayName("CopyFilesWithNoFileInfo")]
        [ChoPropertyInfo("copyFilesWithNoFileInfo")]
        public bool CopyFilesWithNoFileInfo
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("Copy only files with the Archive attribute set. (/A).")]
        [DisplayName("CopyOnlyFilesWithArchiveAttributes")]
        [ChoPropertyInfo("copyOnlyFilesWithArchiveAttributes")]
        public bool CopyOnlyFilesWithArchiveAttributes
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("Copy only files with the Archive attribute and reset it. (/M).")]
        [DisplayName("CopyOnlyFilesWithArchiveAttributesAndReset")]
        [ChoPropertyInfo("copyOnlyFilesWithArchiveAttributesAndReset")]
        public bool CopyOnlyFilesWithArchiveAttributesAndReset
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("Only copy the top n levels of the source directory tree. 0 - all levels. (/LEV:n).")]
        [DisplayName("OnlyCopyNLevels")]
        [ChoPropertyInfo("onlyCopyNLevels")]
        public uint OnlyCopyNLevels
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("MAXimum file AGE - exclude files older than n days/date. (/MAXAGE:n).")]
        [DisplayName("ExcludeFilesOlderThanNDays")]
        [ChoPropertyInfo("excludeFilesOlderThanNDays")]
        public uint ExcludeFilesOlderThanNDays
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("MINimum file AGE - exclude files newer than n days/date. (/MINAGE:n).")]
        [DisplayName("ExcludeFilesNewerThanNDays")]
        [ChoPropertyInfo("excludeFilesNewerThanNDays")]
        public uint ExcludeFilesNewerThanNDays
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("assume FAT File Times (2-second granularity). (/FFT).")]
        [DisplayName("AssumeFATFileTimes")]
        [ChoPropertyInfo("assumeFATFileTimes")]
        public bool AssumeFATFileTimes
        {
            get;
            set;
        }

        [Category("2. Source Options")]
        [Description("Turn off very long path (> 256 characters) support. (/256).")]
        [DisplayName("TurnOffLongPath")]
        [ChoPropertyInfo("turnOffLongPath")]
        public bool TurnOffLongPath
        {
            get;
            set;
        }

        #endregion Instance Data Members (Source Options)

        #region Instance Data Members (Destination Options)

        [Category("3. Destination Options")]
        [Description("Add the given attributes to copied files. (/A+:[RASHCNET]).")]
        [DisplayName("AddFileAttributes")]
        [ChoPropertyInfo("addFileAttributes", DefaultValue = "")]
        [Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        public string AddFileAttributes
        {
            get;
            set;
        }

        [Category("3. Destination Options")]
        [Description("Remove the given Attributes from copied files. (/A-:[RASHCNET]).")]
        [DisplayName("RemoveFileAttributes")]
        [ChoPropertyInfo("removeFileAttributes", DefaultValue = "")]
        [Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        public string RemoveFileAttributes
        {
            get;
            set;
        }

        [Category("3. Destination Options")]
        [Description("Create destination files using 8.3 FAT file names only. (/FAT).")]
        [DisplayName("CreateFATFileNames")]
        [ChoPropertyInfo("createFATFileNames")]
        public bool CreateFATFileNames
        {
            get;
            set;
        }

        [Category("3. Destination Options")]
        [Description("Create directory tree and zero-length files only. (/CREATE).")]
        [DisplayName("CreateDirTree")]
        [ChoPropertyInfo("createDirTree")]
        public bool CreateDirTree
        {
            get;
            set;
        }

        [Category("3. Destination Options")]
        [Description("Compensate for one-hour DST time differences. (/DST).")]
        [DisplayName("CompensateOneHourDSTTimeDiff")]
        [ChoPropertyInfo("compensateOneHourDSTTimeDiff")]
        public bool CompensateOneHourDSTTimeDiff
        {
            get;
            set;
        }

        #endregion Instance Data Members (Destination Options)

        #region Instance Data Members (Copy Options)

        [Category("4. Copy Options")]
        [Description("List only - don't copy, timestamp or delete any files. (/L).")]
        [DisplayName("ListOnly")]
        [ChoPropertyInfo("listOnly")]
        public bool ListOnly
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Move files and dirs (delete from source after copying). (/MOV or /MOVE).")]
        [DisplayName("MoveFilesAndDirectories")]
        [ChoPropertyInfo("moveFilesAndDirectories", DefaultValue = "None")]
        [Editor(typeof(FileMoveSelectionAttributesEditor), typeof(FileMoveSelectionAttributesEditor))]
        public string MoveFilesAndDirectories
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Copy symbolic links versus the target. (/SL).")]
        [DisplayName("CopySymbolicLinks")]
        [ChoPropertyInfo("copySymbolicLinks")]
        public bool CopySymbolicLinks
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Copy files in restartable mode. (/Z).")]
        [DisplayName("CopyFilesRestartableMode")]
        [ChoPropertyInfo("copyFilesRestartableMode")]
        public bool CopyFilesRestartableMode
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Copy files in Backup mode. (/B).")]
        [DisplayName("CopyFilesBackupMode")]
        [ChoPropertyInfo("copyFilesBackupMode")]
        public bool CopyFilesBackupMode
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Copy using unbuffered I/O (recommended for large files). (/J)")]
        [DisplayName("UnbufferredIOCopy")]
        [ChoPropertyInfo("unbufferredIOCopy")]
        public bool UnbufferredIOCopy
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Copy files without using the Windows Copy Offload mechanism. (/NOOFFLOAD).")]
        [DisplayName("CopyWithoutWindowsCopyOffload")]
        [ChoPropertyInfo("copyWithoutWindowsCopyOffload")]
        public bool CopyWithoutWindowsCopyOffload
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Copy all encrypted files in EFS RAW mode. (/EFSRAW).")]
        [DisplayName("EncrptFileEFSRawMode")]
        [ChoPropertyInfo("encrptFileEFSRawMode")]
        public bool EncrptFileEFSRawMode
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Fix file times on all files, even skipped files. (/TIMFIX).")]
        [DisplayName("FixFileTimeOnFiles")]
        [ChoPropertyInfo("fixFileTimeOnFiles")]
        public bool FixFileTimeOnFiles
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude Older files. (/XO).")]
        [DisplayName("ExcludeOlderFiles")]
        [ChoPropertyInfo("excludeOlderFiles")]
        public bool ExcludeOlderFiles
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude Changed files. (/XC).")]
        [DisplayName("ExcludeChangedFiles")]
        [ChoPropertyInfo("excludeChangedFiles")]
        public bool ExcludeChangedFiles
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude Newer files. (/XN).")]
        [DisplayName("ExcludeNewerFiles")]
        [ChoPropertyInfo("excludeNewerFiles")]
        public bool ExcludeNewerFiles
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude eXtra files and directories. (/XX).")]
        [DisplayName("ExcludeExtraFilesAndDirs")]
        [ChoPropertyInfo("excludeExtraFilesAndDirs")]
        public bool ExcludeExtraFilesAndDirs
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude Files matching given names/paths/wildcards. Separate names with ;. (/XF).")]
        [DisplayName("ExcludeFilesWithGivenNames")]
        [ChoPropertyInfo("excludeFilesWithGivenNames", DefaultValue = "")]
        [Editor(typeof(ChoPropertyGridFilePicker), typeof(ChoPropertyGridFilePicker))]
        public string ExcludeFilesWithGivenNames
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude Directories matching given names/paths. Separate names with ;. (/XD).")]
        [DisplayName("ExcludeDirsWithGivenNames")]
        [ChoPropertyInfo("excludeDirsWithGivenNames", DefaultValue = "")]
        [Editor(typeof(ChoPropertyGridFolderPicker), typeof(ChoPropertyGridFolderPicker))]
        public string ExcludeDirsWithGivenNames
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Include only files with any of the given Attributes set. (/IA:[RASHCNETO]).")]
        [DisplayName("IncludeFilesWithGivenAttributes")]
        [ChoPropertyInfo("includeFilesWithGivenAttributes", DefaultValue = "")]
        [Editor(typeof(FileSelectionAttributesEditor), typeof(FileSelectionAttributesEditor))]
        public string IncludeFilesWithGivenAttributes
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude files with any of the given Attributes set. (/XA:[RASHCNETO]).")]
        [DisplayName("ExcludeFilesWithGivenAttributes")]
        [ChoPropertyInfo("excludeFilesWithGivenAttributes", DefaultValue = "")]
        [Editor(typeof(FileSelectionAttributesEditor), typeof(FileSelectionAttributesEditor))]
        public string ExcludeFilesWithGivenAttributes
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Include Modified files (differing change times). Otherwise the same. These files are not copied by default;. (/IM).")]
        [DisplayName("OverrideModifiedFiles")]
        [ChoPropertyInfo("overrideModifiedFiles")]
        public bool OverrideModifiedFiles
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Include Same files. Overwrite files even if they are already the same. (/IS).")]
        [DisplayName("IncludeSameFiles")]
        [ChoPropertyInfo("includeSameFiles")]
        public bool IncludeSameFiles
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Include Tweaked files. (/IT).")]
        [DisplayName("IncludeTweakedFiles")]
        [ChoPropertyInfo("includeTweakedFiles")]
        public bool IncludeTweakedFiles
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude Junction points and symbolic links. (normally included by default). (/XJ).")]
        [DisplayName("ExcludeJunctionPoints")]
        [ChoPropertyInfo("excludeJunctionPoints")]
        public bool ExcludeJunctionPoints
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude Junction points and symbolic links for source directories. (/XJD).")]
        [DisplayName("ExcludeJunctionPointsForDirs")]
        [ChoPropertyInfo("excludeJunctionPointsForDirs")]
        public bool ExcludeJunctionPointsForDirs
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude symbolic links for source files. (/XJF).")]
        [DisplayName("ExcludeJunctionPointsForFiles")]
        [ChoPropertyInfo("excludeJunctionPointsForFiles")]
        public bool ExcludeJunctionPointsForFiles
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("MAXimum file size - exclude files bigger than n bytes. (/MAX:n).")]
        [DisplayName("ExcludeFilesBiggerThanNBytes")]
        [ChoPropertyInfo("excludeFilesBiggerThanNBytes")]
        public uint ExcludeFilesBiggerThanNBytes
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("MINimum file size - exclude files smaller than n bytes. (/MIN:n).")]
        [DisplayName("ExcludeFilesSmallerThanNBytes")]
        [ChoPropertyInfo("excludeFilesSmallerThanNBytes")]
        public uint ExcludeFilesSmallerThanNBytes
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("MAXimum Last Access Date - exclude files unused since n. (/MAXLAD:n).")]
        [DisplayName("ExcludeFilesUnusedSinceNDays")]
        [ChoPropertyInfo("excludeFilesUnusedSinceNDays")]
        public uint ExcludeFilesUnusedSinceNDays
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("MINimum Last Access Date - exclude files used since n. (If n < 1900 then n = n days, else n = YYYYMMDD date). (/MINLAD:n).")]
        [DisplayName("ExcludeFilesUsedSinceNDays")]
        [ChoPropertyInfo("excludeFilesUsedSinceNDays")]
        public uint ExcludeFilesUsedSinceNDays
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Mirror a directory tree (equivalent to /E plus /PURGE). (/MIR).")]
        [DisplayName("MirrorDirTree")]
        [ChoPropertyInfo("mirrorDirTree")]
        public bool MirrorDirTree
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Delete dest files/dirs that no longer exist in source. (/PURGE).")]
        [DisplayName("DelDestFileDirIfNotExistsInSource")]
        [ChoPropertyInfo("delDestFileDirIfNotExistsInSource")]
        public bool DelDestFileDirIfNotExistsInSource
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("eXclude Lonely files and directories. (/XL).")]
        [DisplayName("ExcludeLonelyFilesAndDirs")]
        [ChoPropertyInfo("excludeLonelyFilesAndDirs")]
        public bool ExcludeLonelyFilesAndDirs
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Fix file security on all files, even skipped files. (/SECFIX).")]
        [DisplayName("FixFileSecurityOnFiles")]
        [ChoPropertyInfo("fixFileSecurityOnFiles")]
        public bool FixFileSecurityOnFiles
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Use restartable mode; if access denied use Backup mode. (/ZB).")]
        [DisplayName("FallbackCopyFilesMode")]
        [ChoPropertyInfo("fallbackCopyFilesMode")]
        public bool FallbackCopyFilesMode
        {
            get;
            set;
        }

        [Category("4. Copy Options")]
        [Description("Inter-Packet Gap (ms), to free bandwidth on slow lines. (/IPG:n).")]
        [DisplayName("InterPacketGapInMS")]
        [ChoPropertyInfo("interPacketGapInMS")]
        public uint InterPacketGapInMS
        {
            get;
            set;
        }

        private uint _multithreadCopy;
        [Category("4. Copy Options")]
        [Description("Do multi-threaded copies with n threads (default 8). n must be at least 1 and not greater than 128. This option is incompatible with the /IPG and /EFSRAW options. Redirect output using /LOG option for better performance. (/MT[:n]).")]
        [DisplayName("MultithreadCopy")]
        [ChoPropertyInfo("multithreadCopy")]
        public uint MultithreadCopy
        {
            get { return _multithreadCopy; }
            set
            {
                if (value < 1 || value > 128)
                    _multithreadCopy = 0;
                else
                    _multithreadCopy = value;
            }
        }

        [Category("4. Copy Options")]
        [Description("COPY NO directory info (by default /DCOPY:DA is done). (/NODCOPY).")]
        [DisplayName("CopyNODirInfo")]
        [ChoPropertyInfo("copyNODirInfo")]
        public bool CopyNODirInfo
        {
            get;
            set;
        }
        #endregion Instance Data Members (Copy Options)

        #region Instance Data Members (Monitoring Options)

        const string DefaultNoOfRetries = "1000000";

        [Category("5. Monitoring Options")]
        [Description("Number of Retries on failed copies: default 1 million. (/R:n).")]
        [DisplayName("NoOfRetries")]
        [ChoPropertyInfo("noOfRetries", DefaultValue = DefaultNoOfRetries)]
        public uint NoOfRetries
        {
            get;
            set;
        }

        const string DefaultWaitTimeBetweenRetries = "30";

        [Category("5. Monitoring Options")]
        [Description("Wait time between retries: default is 30 seconds. (/W:n).")]
        [DisplayName("WaitTimeBetweenRetries")]
        [ChoPropertyInfo("waitTimeBetweenRetries", DefaultValue = DefaultWaitTimeBetweenRetries)]
        public uint WaitTimeBetweenRetries
        {
            get;
            set;
        }

        [Category("5. Monitoring Options")]
        [Description("Save /R:n and /W:n in the Registry as default settings. (/REG).")]
        [DisplayName("SaveRetrySettingsToRegistry")]
        [ChoPropertyInfo("saveRetrySettingsToRegistry")]
        public bool SaveRetrySettingsToRegistry
        {
            get;
            set;
        }

        [Category("5. Monitoring Options")]
        [Description("Wait for sharenames to be defined (retry error 67). (/TBD).")]
        [DisplayName("WaitForSharenames")]
        [ChoPropertyInfo("waitForSharenames")]
        public bool WaitForSharenames
        {
            get;
            set;
        }

        [Category("5. Monitoring Options")]
        [Description("Monitor source; run again when more than n changes seen. (/MON:n).")]
        [DisplayName("RunAgainWithNoChangesSeen")]
        [ChoPropertyInfo("runAgainWithNoChangesSeen")]
        public uint RunAgainWithNoChangesSeen
        {
            get;
            set;
        }

        [Category("5. Monitoring Options")]
        [Description("Monitor source; run again in m minutes time, if changed. (/MOT:m).")]
        [DisplayName("RunAgainWithChangesSeenInMin")]
        [ChoPropertyInfo("runAgainWithChangesSeenInMin")]
        public uint RunAgainWithChangesSeenInMin
        {
            get;
            set;
        }

        [Category("5. Monitoring Options")]
        [Description("Check run hours on a Per File (not per pass) basis. (/PF).")]
        [DisplayName("CheckRunHourPerFileBasis")]
        [ChoPropertyInfo("checkRunHourPerFileBasis")]
        public bool CheckRunHourPerFileBasis
        {
            get;
            set;
        }

        #endregion Instance Data Members (Monitoring Options)

        #region Instance Data Members (Scheduling Options)

        private TimeSpan _runHourStartTime;
        [Category("6. Scheduling Options")]
        [Description("Run Hours StartTime, when new copies may be started after then. (/RH:hhmm-hhmm).")]
        [DisplayName("RunHourStartTime")]
        [ChoPropertyInfo("runHourStartTime")]
        [XmlIgnore]
        public TimeSpan RunHourStartTime
        {
            get { return _runHourStartTime; }
            set { _runHourStartTime = value; }
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
        public TimeSpan RunHourEndTime
        {
            get { return _runHourEndTime; }
            set { _runHourEndTime = value; }
        }

        [Browsable(false)]
        public long RunHourEndTimeTicks
        {
            get { return _runHourEndTime.Ticks; }
            set { _runHourEndTime = new TimeSpan(value); }
        }


        #endregion Instance Data Members (Scheduling Options)

        #region Instance Data Members (Logging Options)

        [Category("7. Logging Options")]
        [Description("No Progress - don't display percentage copied. Suppresses the display of progress information. This can be useful when output is redirected to a file. (/NP).")]
        [DisplayName("NoProgress")]
        [ChoPropertyInfo("noProgress")]
        public bool NoProgress
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Display the status output as unicode text. (/unicode).")]
        [DisplayName("Unicode")]
        [ChoPropertyInfo("unicode")]
        public bool Unicode
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Output status to LOG file (overwrite existing log). (/LOG:file).")]
        [DisplayName("OutputLogFilePath")]
        [ChoPropertyInfo("outputLogFilePath", DefaultValue = "")]
        public string OutputLogFilePath
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Output status to LOG file as UNICODE (overwrite existing log). (/UNILOG:file).")]
        [DisplayName("UnicodeOutputLogFilePath")]
        [ChoPropertyInfo("unicodeOutputLogFilePath", DefaultValue = "")]
        public string UnicodeOutputLogFilePath
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Output status to LOG file (append to existing log). (/LOG+:file).")]
        [DisplayName("AppendOutputLogFilePath")]
        [ChoPropertyInfo("appendOutputLogFilePath", DefaultValue = "")]
        public string AppendOutputLogFilePath
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Output status to LOG file as UNICODE (append to existing log). (/UNILOG+:file).")]
        [DisplayName("AppendUnicodeOutputLogFilePath")]
        [ChoPropertyInfo("appendUnicodeOutputLogFilePath", DefaultValue = "")]
        public string AppendUnicodeOutputLogFilePath
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Include source file timestamps in the output. (/TS).")]
        [DisplayName("IncludeSourceFileTimestamp")]
        [ChoPropertyInfo("includeSourceFileTimestamp")]
        public bool IncludeSourceFileTimestamp
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Include Full Pathname of files in the output. (/FP).")]
        [DisplayName("IncludeFullPathName")]
        [ChoPropertyInfo("includeFullPathName")]
        public bool IncludeFullPathName
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("No Size - don't log file sizes. (/NS).")]
        [DisplayName("NoFileSizeLog")]
        [ChoPropertyInfo("noFileSizeLog")]
        public bool NoFileSizeLog
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("No Class - don't log file classes. (/NC).")]
        [DisplayName("NoFileClassLog")]
        [ChoPropertyInfo("noFileClassLog")]
        public bool NoFileClassLog
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("No File List - don't log file names. Hides file names. Failures are still logged though. Any files files deleted or would be deleted if /L was omitted are always logged. (/NFL).")]
        [DisplayName("NoFileNameLog")]
        [ChoPropertyInfo("noFileNameLog")]
        public bool NoFileNameLog
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("No Directory List - don't log directory names. Hides output of the directory listing. Full file pathnames are output to more easily track down problematic files. (/NDL).")]
        [DisplayName("NoDirListLog")]
        [ChoPropertyInfo("noDirListLog")]
        public bool NoDirListLog
        {
            get;
            set;
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

        [Category("7. Logging Options")]
        [Description("No Job Header. (/NJH).")]
        [DisplayName("NoJobHeader")]
        [ChoPropertyInfo("noJobHeader")]
        public bool NoJobHeader
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("No Job Summary. (/NJS).")]
        [DisplayName("NoJobSummary")]
        [ChoPropertyInfo("noJobSummary")]
        public bool NoJobSummary
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Print sizes as bytes. (/BYTES).")]
        [DisplayName("PrintByteSizes")]
        [ChoPropertyInfo("printByteSizes")]
        public bool PrintByteSizes
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Report all eXtra files, not just those selected. (/X).")]
        [DisplayName("ReportExtraFiles")]
        [ChoPropertyInfo("reportExtraFiles")]
        public bool ReportExtraFiles
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Produce Verbose output, showing skipped files. (/V).")]
        [DisplayName("VerboseOutput")]
        [ChoPropertyInfo("verboseOutput")]
        public bool VerboseOutput
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Show Estimated Time of Arrival of copied files. (/ETA).")]
        [DisplayName("ShowEstTimeOfArrival")]
        [ChoPropertyInfo("showEstTimeOfArrival")]
        public bool ShowEstTimeOfArrival
        {
            get;
            set;
        }

        [Category("7. Logging Options")]
        [Description("Show debug volume information. (/DEBUG).")]
        [DisplayName("ShowDebugVolumeInfo")]
        [ChoPropertyInfo("showDebugVolumeInfo")]
        public bool ShowDebugVolumeInfo
        {
            get;
            set;
        }

        #endregion Instance Data Members (Logging Options)

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

        public void Reset()
        {
            ChoObject.ResetObject(this);
            Persist();
            MultithreadCopy = 8;
            Precommands = null;
            Postcommands = null;
            Comments = null;
        }
        
        internal string GetCmdLineText()
        {
            return "{0} {1}".FormatString(RoboCopyFilePath, GetCmdLineParams());
        }

        internal string GetCmdLineTextEx()
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
            if (!CopyFlags.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /COPY:{0}", (from f in CopyFlags.SplitNTrim()
                                                    where !f.IsNullOrWhiteSpace()
                                                    select ((ChoCopyFlags)Enum.Parse(typeof(ChoCopyFlags), f)).ToDescription()).Join(""));
            }
            if (CopyDirTimestamp)
                cmdText.Append(" /DCOPY:T");
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
            if (!MoveFilesAndDirectories.IsNullOrWhiteSpace())
            {
                ChoFileMoveAttributes value = ChoFileMoveAttributes.None;
                if (Enum.TryParse<ChoFileMoveAttributes>(MoveFilesAndDirectories, out value))
                {
                    switch (value)
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
                }
            }
            if (!AddFileAttributes.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /A+:{0}", (from f in AddFileAttributes.SplitNTrim()
                                                  where !f.IsNullOrWhiteSpace()
                                                  select ((ChoFileAttributes)Enum.Parse(typeof(ChoFileAttributes), f)).ToDescription()).Join(""));
            }
            if (!RemoveFileAttributes.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /A-:{0}", (from f in RemoveFileAttributes.SplitNTrim()
                                                  where !f.IsNullOrWhiteSpace()
                                                  select ((ChoFileAttributes)Enum.Parse(typeof(ChoFileAttributes), f)).ToDescription()).Join(""));
            }
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
            if (!IncludeFilesWithGivenAttributes.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /IA:{0}", (from f in IncludeFilesWithGivenAttributes.SplitNTrim()
                                                  where !f.IsNullOrWhiteSpace()
                                                  select ((ChoFileSelectionAttributes)Enum.Parse(typeof(ChoFileSelectionAttributes), f)).ToDescription()).Join(""));
            }
            if (!ExcludeFilesWithGivenAttributes.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /XA:{0}", (from f in ExcludeFilesWithGivenAttributes.SplitNTrim()
                                                  where !f.IsNullOrWhiteSpace()
                                                  select ((ChoFileSelectionAttributes)Enum.Parse(typeof(ChoFileSelectionAttributes), f)).ToDescription()).Join(""));
            }
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

        protected override void OnAfterConfigurationObjectLoaded()
        {
            if (RoboCopyFilePath.IsNullOrWhiteSpace())
                RoboCopyFilePath = "RoboCopy.exe";
        }
    }

    //Custom editors that are used as attributes MUST implement the ITypeEditor interface.
    public class CopyFlagsEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoMultiSelectComboBox cmb = new ChoMultiSelectComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoMultiSelectComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoCopyFlags>(propertyItem.Value.ToNString());

            return cmb;
        }
    }

    //Custom editors that are used as attributes MUST implement the ITypeEditor interface.
    public class FileAttributesEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoMultiSelectComboBox cmb = new ChoMultiSelectComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoMultiSelectComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoFileAttributes>(propertyItem.Value.ToNString());

            return cmb;
        }
    }

    //Custom editors that are used as attributes MUST implement the ITypeEditor interface.
    public class FileSelectionAttributesEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoMultiSelectComboBox cmb = new ChoMultiSelectComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoMultiSelectComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoFileSelectionAttributes>(propertyItem.Value.ToNString());

            return cmb;
        }
    }

    public class FileMoveSelectionAttributesEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoFileMoveComboBox cmb = new ChoFileMoveComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoFileMoveComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoFileMoveAttributes>(propertyItem.Value.ToNString()).Select(c => c.Title);
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => cmb.SelectedItem = propertyItem.Value.ToNString()));
            return cmb;
        }
    }

    public class ChoFileMoveComboBox : ComboBox
    {
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                if (e.AddedItems != null && e.AddedItems.Count > 0)
                {
                    var value = (ChoFileMoveAttributes)Enum.Parse(typeof(ChoFileMoveAttributes), e.AddedItems.OfType<string>().FirstOrDefault());
                    if (value == ChoFileMoveAttributes.MoveFilesOnly)
                    {
                        if (MessageBox.Show("Would like to delete the original file(s) after transferring the copies to the new location?", MainWindow.Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (value == ChoFileMoveAttributes.MoveDirectoriesAndFiles)
                    {
                        if (MessageBox.Show("Would like to delete the original file(s) / folder(s) after transferring the copies to the new location?", MainWindow.Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
            base.OnSelectionChanged(e);
        }
    }
}
