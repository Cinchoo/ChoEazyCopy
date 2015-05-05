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
        Normal,
        [Description("E")]
        Encrypted,
        [Description("T")]
        Temporary
    }

    [ChoNameValueConfigurationSection("applicationSettings" /*, BindingMode = ChoConfigurationBindingMode.OneWayToSource */, Silent = false)]
    public class ChoAppSettings : ChoConfigurableObject
    {
        #region Instance Data Members (Others)

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

        [Category("Common Options")]
        [Description("RoboCopy file path.")]
        [DisplayName("RoboCopyFilePath")]
        [ChoPropertyInfo("roboCopyFilePath", DefaultValue = "RoboCopy.exe")]
        public string RoboCopyFilePath
        {
            get;
            set;
        }

        [Category("Common Options")]
        [Description("File(s) to copy (names/wildcards: default is \"*.*\").")]
        [DisplayName("Files")]
        [ChoPropertyInfo("files", DefaultValue = "*.*")]
        public string Files
        {
            get;
            set;
        }
        
        #endregion Instance Data Members (Common Options)

        #region Instance Data Members (Copy Options)

        [Category("Copy Options")]
        [Description("Copy subdirectories, but not empty ones. (/S).")]
        [DisplayName("CopyNoEmptySubDirectories")]
        [ChoPropertyInfo("copyNoEmptySubDirectories")]
        public bool CopyNoEmptySubDirectories
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy subdirectories, including Empty ones. (/E).")]
        [DisplayName("CopyEmptySubDirectories")]
        [ChoPropertyInfo("copyEmptySubDirectories")]
        public bool CopyEmptySubDirectories
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Only copy the top n levels of the source directory tree. 0 - all levels. (/LEV:n).")]
        [DisplayName("OnlyCopyNLevels")]
        [ChoPropertyInfo("onlyCopyNLevels")]
        public uint OnlyCopyNLevels
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy files in restartable mode. (/Z).")]
        [DisplayName("CopyFilesRestartableMode")]
        [ChoPropertyInfo("copyFilesRestartableMode")]
        public bool CopyFilesRestartableMode
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy files in Backup mode. (/B).")]
        [DisplayName("CopyFilesBackupMode")]
        [ChoPropertyInfo("copyFilesBackupMode")]
        public bool CopyFilesBackupMode
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Use restartable mode; if access denied use Backup mode. (/ZB).")]
        [DisplayName("FallbackCopyFilesMode")]
        [ChoPropertyInfo("fallbackCopyFilesMode")]
        public bool FallbackCopyFilesMode
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy using unbuffered I/O (recommended for large files).")]
        [DisplayName("UnbufferredIOCopy")]
        [ChoPropertyInfo("unbufferredIOCopy")]
        public bool UnbufferredIOCopy
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy all encrypted files in EFS RAW mode. (/EFSRAW).")]
        [DisplayName("EncrptFileEFSRawMode")]
        [ChoPropertyInfo("encrptFileEFSRawMode")]
        public bool EncrptFileEFSRawMode
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("What to copy for files (default is /COPY:DAT). (copyflags : D=Data, A=Attributes, T=Timestamps, S=Security=NTFS ACLs, O=Owner info, U=aUditing info). (/COPY:flags).")]
        [DisplayName("CopyFlags")]
        [ChoPropertyInfo("copyFlags")]
        [Editor(typeof(CopyFlagsEditor), typeof(CopyFlagsEditor))]
        public string CopyFlags
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy Directory Timestamps. (/DCOPY:T).")]
        [DisplayName("CopyDirTimestamp")]
        [ChoPropertyInfo("copyDirTimestamp")]
        public bool CopyDirTimestamp
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy files with security (equivalent to /COPY:DATS). (/SEC).")]
        [DisplayName("CopyFilesWithSecurity")]
        [ChoPropertyInfo("copyFilesWithSecurity")]
        public bool CopyFilesWithSecurity
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy all file info (equivalent to /COPY:DATSOU). (/COPYALL).")]
        [DisplayName("CopyFilesWithFileInfo")]
        [ChoPropertyInfo("copyFilesWithFileInfo")]
        public bool CopyFilesWithFileInfo
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy no file info (useful with /PURGE). (/NOCOPY).")]
        [DisplayName("CopyFilesWithNoFileInfo")]
        [ChoPropertyInfo("copyFilesWithNoFileInfo")]
        public bool CopyFilesWithNoFileInfo
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Fix file security on all files, even skipped files. (/SECFIX).")]
        [DisplayName("FixFileSecurityOnFiles")]
        [ChoPropertyInfo("fixFileSecurityOnFiles")]
        public bool FixFileSecurityOnFiles
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Fix file times on all files, even skipped files. (/TIMFIX).")]
        [DisplayName("FixFileTimeOnFiles")]
        [ChoPropertyInfo("fixFileTimeOnFiles")]
        public bool FixFileTimeOnFiles
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Delete dest files/dirs that no longer exist in source. (/PURGE).")]
        [DisplayName("DelDestFileDirIfNotExistsInSource")]
        [ChoPropertyInfo("delDestFileDirIfNotExistsInSource")]
        public bool DelDestFileDirIfNotExistsInSource
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Mirror a directory tree (equivalent to /E plus /PURGE). (/MIR).")]
        [DisplayName("MirrorDirTree")]
        [ChoPropertyInfo("mirrorDirTree")]
        public bool MirrorDirTree
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Move files (delete from source after copying). (/MOV).")]
        [DisplayName("MoveFiles")]
        [ChoPropertyInfo("moveFiles")]
        public bool MoveFiles
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Move files and dirs (delete from source after copying). (/MOVE).")]
        [DisplayName("MoveFilesNDirs")]
        [ChoPropertyInfo("moveFilesNDirs")]
        public bool MoveFilesNDirs
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Add the given attributes to copied files. (/A+:[RASHCNET]).")]
        [DisplayName("AddFileAttributes")]
        [ChoPropertyInfo("addFileAttributes")]
        [Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        public string AddFileAttributes
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Remove the given Attributes from copied files. (/A-:[RASHCNET]).")]
        [DisplayName("RemoveFileAttributes")]
        [ChoPropertyInfo("removeFileAttributes")]
        [Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        public string RemoveFileAttributes
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Create directory tree and zero-length files only. (/CREATE).")]
        [DisplayName("CreateDirTree")]
        [ChoPropertyInfo("createDirTree")]
        public bool CreateDirTree
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Create destination files using 8.3 FAT file names only. (/FAT).")]
        [DisplayName("CreateFATFileNames")]
        [ChoPropertyInfo("createFATFileNames")]
        public bool CreateFATFileNames
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Turn off very long path (> 256 characters) support. (/256).")]
        [DisplayName("TurnOffLongPath")]
        [ChoPropertyInfo("turnOffLongPath")]
        public bool TurnOffLongPath
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Monitor source; run again when more than n changes seen. (/MON:n).")]
        [DisplayName("RunAgainWithNoChangesSeen")]
        [ChoPropertyInfo("runAgainWithNoChangesSeen")]
        public uint RunAgainWithNoChangesSeen
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Monitor source; run again in m minutes time, if changed. (/MOT:m).")]
        [DisplayName("RunAgainWithChangesSeenInMin")]
        [ChoPropertyInfo("runAgainWithChangesSeenInMin")]
        public uint RunAgainWithChangesSeenInMin
        {
            get;
            set;
        }

        private TimeSpan _runHourStartTime;
        [Category("Copy Options")]
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
        [Category("Copy Options")]
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

        [Category("Copy Options")]
        [Description("Check run hours on a Per File (not per pass) basis. (/PF).")]
        [DisplayName("CheckRunHourPerFileBasis")]
        [ChoPropertyInfo("checkRunHourPerFileBasis")]
        public bool CheckRunHourPerFileBasis
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Inter-Packet Gap (ms), to free bandwidth on slow lines. (/IPG:n).")]
        [DisplayName("InterPacketGapInMS")]
        [ChoPropertyInfo("interPacketGapInMS")]
        public uint InterPacketGapInMS
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy symbolic links versus the target. (/SL).")]
        [DisplayName("CopySymbolicLinks")]
        [ChoPropertyInfo("copySymbolicLinks")]
        public bool CopySymbolicLinks
        {
            get;
            set;
        }

        private uint _multithreadCopy;
        [Category("Copy Options")]
        [Description("Do multi-threaded copies with n threads (default 8). n must be at least 1 and not greater than 128. This option is incompatible with the /IPG and /EFSRAW options. Redirect output using /LOG option for better performance. (/MT[:n]).")]
        [DisplayName("MultithreadCopy")]
        [ChoPropertyInfo("multithreadCopy")]
        public uint MultithreadCopy
        {
            get { return _multithreadCopy; }
            set
            {
                if (value < 1 || value > 128)
                    _multithreadCopy = 8;
                else
                    _multithreadCopy = value;
            }
        }

        #endregion Instance Data Members (Copy Options)

        #region Instance Data Members (Retry Options)

        [Category("Retry Options")]
        [Description("Number of Retries on failed copies: default 1 million. (/R:n).")]
        [DisplayName("NoOfRetries")]
        [ChoPropertyInfo("noOfRetries")]
        public uint NoOfRetries
        {
            get;
            set;
        }

        [Category("Retry Options")]
        [Description("Wait time between retries: default is 30 seconds. (/W:n).")]
        [DisplayName("WaitTimeBetweenRetries")]
        [ChoPropertyInfo("waitTimeBetweenRetries")]
        public uint WaitTimeBetweenRetries
        {
            get;
            set;
        }

        [Category("Retry Options")]
        [Description("Save /R:n and /W:n in the Registry as default settings. (/REG).")]
        [DisplayName("SaveRetrySettingsToRegistry")]
        [ChoPropertyInfo("saveRetrySettingsToRegistry")]
        public bool SaveRetrySettingsToRegistry
        {
            get;
            set;
        }

        [Category("Retry Options")]
        [Description("Wait for sharenames to be defined (retry error 67). (/TBD).")]
        [DisplayName("WaitForSharenames")]
        [ChoPropertyInfo("waitForSharenames")]
        public bool WaitForSharenames
        {
            get;
            set;
        }

        #endregion Instance Data Members (Retry Options)

        #region Instance Data Members (Logging Options)

        [Category("Logging Options")]
        [Description("List only - don't copy, timestamp or delete any files. (/L).")]
        [DisplayName("ListOnly")]
        [ChoPropertyInfo("listOnly")]
        public bool ListOnly
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Report all eXtra files, not just those selected. (/X).")]
        [DisplayName("ReportExtraFiles")]
        [ChoPropertyInfo("reportExtraFiles")]
        public bool ReportExtraFiles
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Produce Verbose output, showing skipped files. (/V).")]
        [DisplayName("VerboseOutput")]
        [ChoPropertyInfo("verboseOutput")]
        public bool VerboseOutput
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Include source file Time Stamps in the output. (/TS).")]
        [DisplayName("IncludeSourceFileTimestamp")]
        [ChoPropertyInfo("includeSourceFileTimestamp")]
        public bool IncludeSourceFileTimestamp
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Include Full Pathname of files in the output. (/FP).")]
        [DisplayName("IncludeFullPathName")]
        [ChoPropertyInfo("includeFullPathName")]
        public bool IncludeFullPathName
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Print sizes as bytes. (/BYTES).")]
        [DisplayName("PrintByteSizes")]
        [ChoPropertyInfo("printByteSizes")]
        public bool PrintByteSizes
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("No Size - don't log file sizes. (/NS).")]
        [DisplayName("NoFileSizeLog")]
        [ChoPropertyInfo("noFileSizeLog")]
        public bool NoFileSizeLog
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("No Class - don't log file classes. (/NC).")]
        [DisplayName("NoFileClassLog")]
        [ChoPropertyInfo("noFileClassLog")]
        public bool NoFileClassLog
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("No File List - don't log file names. (/NFL).")]
        [DisplayName("NoFileNameLog")]
        [ChoPropertyInfo("noFileNameLog")]
        public bool NoFileNameLog
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("No Directory List - don't log directory names. (/NDL).")]
        [DisplayName("NoDirListLog")]
        [ChoPropertyInfo("noDirListLog")]
        public bool NoDirListLog
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("No Progress - don't display percentage copied. (/NP).")]
        [DisplayName("NoProgress")]
        [ChoPropertyInfo("noProgress")]
        public bool NoProgress
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Show Estimated Time of Arrival of copied files. (/ETA).")]
        [DisplayName("ShowEstTimeOfArrival")]
        [ChoPropertyInfo("showEstTimeOfArrival")]
        public bool ShowEstTimeOfArrival
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Output status to LOG file (overwrite existing log). (/LOG:file).")]
        [DisplayName("OutputLogFilePath")]
        [ChoPropertyInfo("outputLogFilePath")]
        public string OutputLogFilePath
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Output status to LOG file (append to existing log). (/LOG+:file).")]
        [DisplayName("AppendOutputLogFilePath")]
        [ChoPropertyInfo("appendOutputLogFilePath")]
        public string AppendOutputLogFilePath
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Output status to LOG file as UNICODE (overwrite existing log). (/UNILOG:file).")]
        [DisplayName("UnicodeOutputLogFilePath")]
        [ChoPropertyInfo("unicodeOutputLogFilePath")]
        public string UnicodeOutputLogFilePath
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("Output status to LOG file as UNICODE (append to existing log). (/UNILOG+:file).")]
        [DisplayName("AppendUnicodeOutputLogFilePath")]
        [ChoPropertyInfo("appendUnicodeOutputLogFilePath")]
        public string AppendUnicodeOutputLogFilePath
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

        [Category("Logging Options")]
        [Description("No Job Header. (/NJH).")]
        [DisplayName("NoJobHeader")]
        [ChoPropertyInfo("noJobHeader")]
        public bool NoJobHeader
        {
            get;
            set;
        }

        [Category("Logging Options")]
        [Description("No Job Summary. (/NJS).")]
        [DisplayName("NoJobSummary")]
        [ChoPropertyInfo("noJobSummary")]
        public bool NoJobSummary
        {
            get;
            set;
        }

        #endregion Instance Data Members (Logging Options)

        public void Reset()
        {
            ChoObject.ResetObject(this);
            Persist();
        }
        
        internal string GetCmdLineText()
        {
            return "{0} {1}".FormatString(RoboCopyFilePath, GetCmdLineParams());
        }

        internal string GetCmdLineParams()
        {
            StringBuilder cmdText = new StringBuilder();

            if (!SourceDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", SourceDirectory);
            if (!DestDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", DestDirectory);
            if (!Files.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" {0}", Files);
            else
                cmdText.Append("*.*");

            if (CopyNoEmptySubDirectories)
                cmdText.Append(" /S");
            if (CopyEmptySubDirectories)
                cmdText.Append(" /E");
            if (OnlyCopyNLevels > 0)
                cmdText.AppendFormat(" /LEV:{0}", OnlyCopyNLevels);
            if (CopyFilesRestartableMode)
                cmdText.Append(" /Z");
            if (CopyFilesBackupMode)
                cmdText.Append(" /B");
            if (FallbackCopyFilesMode)
                cmdText.Append(" /ZB");

            //if (UnbufferredIOCopy)
            //    cmdText.Append(" /E");

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
            if (MoveFiles)
                cmdText.Append(" /MOV");
            if (MoveFilesNDirs)
                cmdText.Append(" /MOVE");
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
            if (MultithreadCopy > 0)
                cmdText.AppendFormat(" /MT:{0}", MultithreadCopy);

            if (NoOfRetries > 0)
                cmdText.AppendFormat(" /R:{0}", NoOfRetries);
            if (WaitTimeBetweenRetries > 0)
                cmdText.AppendFormat(" /W:{0}", WaitTimeBetweenRetries);
            if (SaveRetrySettingsToRegistry)
                cmdText.Append(" /REG");
            if (WaitForSharenames)
                cmdText.Append(" /TBD");

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
            if (ShowEstTimeOfArrival)
                cmdText.Append(" /ETA");
            if (!OutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /LOG:{0}", OutputLogFilePath);
            if (!AppendOutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /LOG+:{0}", AppendOutputLogFilePath);
            if (!UnicodeOutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /UNILOG:{0}", UnicodeOutputLogFilePath);
            if (!AppendUnicodeOutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /UNILOG+:{0}", AppendUnicodeOutputLogFilePath);
            if (NoJobHeader)
                cmdText.Append(" /NJH");
            if (NoJobSummary)
                cmdText.Append(" /NJS");

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
}
