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
        Data,
        Attributes,
        Timestamps,
        SecurityNTFSACLs,
        OwnerInfo,
        AuditingInfo
    }

    public enum ChoFileAttributes
    {
        ReadOnly,
        Hidden,
        Archive,
        System,
        Compressed,
        Normal,
        Encrypted,
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

        #region Instance Data Members (Copy Options)

        [Category("Copy Options")]
        [Description("Copy subdirectories, but not empty ones.")]
        [DisplayName("CopyNoEmptySubDirectories")]
        [ChoPropertyInfo("copyNoEmptySubDirectories")]
        public bool CopyNoEmptySubDirectories
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy subdirectories, including Empty ones.")]
        [DisplayName("CopyEmptySubDirectories")]
        [ChoPropertyInfo("copyEmptySubDirectories")]
        public bool CopyEmptySubDirectories
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Only copy the top n levels of the source directory tree. 0 - all levels.")]
        [DisplayName("OnlyCopyNLevels")]
        [ChoPropertyInfo("onlyCopyNLevels")]
        public uint OnlyCopyNLevels
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy files in restartable mode.")]
        [DisplayName("CopyFilesRestartableMode")]
        [ChoPropertyInfo("copyFilesRestartableMode")]
        public bool CopyFilesRestartableMode
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy files in Backup mode.")]
        [DisplayName("CopyFilesBackupMode")]
        [ChoPropertyInfo("copyFilesBackupMode")]
        public bool CopyFilesBackupMode
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Use restartable mode; if access denied use Backup mode.")]
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
        [Description("Copy all encrypted files in EFS RAW mode.")]
        [DisplayName("EncrptFileEFSRawMode")]
        [ChoPropertyInfo("encrptFileEFSRawMode")]
        public bool EncrptFileEFSRawMode
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("What to copy for files (default is /COPY:DAT). (copyflags : D=Data, A=Attributes, T=Timestamps, S=Security=NTFS ACLs, O=Owner info, U=aUditing info).")]
        [DisplayName("CopyFlags")]
        [ChoPropertyInfo("copyFlags")]
        [Editor(typeof(CopyFlagsEditor), typeof(CopyFlagsEditor))]
        public string CopyFlags
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy files with security (equivalent to /COPY:DATS).")]
        [DisplayName("CopyFilesWithSecurity")]
        [ChoPropertyInfo("copyFilesWithSecurity")]
        public bool CopyFilesWithSecurity
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy all file info (equivalent to /COPY:DATSOU).")]
        [DisplayName("CopyFilesWithFileInfo")]
        [ChoPropertyInfo("copyFilesWithFileInfo")]
        public bool CopyFilesWithFileInfo
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Copy no file info (useful with /PURGE).")]
        [DisplayName("CopyFilesWithNoFileInfo")]
        [ChoPropertyInfo("copyFilesWithNoFileInfo")]
        public bool CopyFilesWithNoFileInfo
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Fix file security on all files, even skipped files.")]
        [DisplayName("FixFileSecurityOnFiles")]
        [ChoPropertyInfo("fixFileSecurityOnFiles")]
        public bool FixFileSecurityOnFiles
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Fix file times on all files, even skipped files.")]
        [DisplayName("FixFileTimeOnFiles")]
        [ChoPropertyInfo("fixFileTimeOnFiles")]
        public bool FixFileTimeOnFiles
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Delete dest files/dirs that no longer exist in source.")]
        [DisplayName("DelDestFileDirIfNotExistsInSource")]
        [ChoPropertyInfo("delDestFileDirIfNotExistsInSource")]
        public bool DelDestFileDirIfNotExistsInSource
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Mirror a directory tree (equivalent to /E plus /PURGE).")]
        [DisplayName("MirrorDirTree")]
        [ChoPropertyInfo("mirrorDirTree")]
        public bool MirrorDirTree
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Move files (delete from source after copying).")]
        [DisplayName("MoveFiles")]
        [ChoPropertyInfo("moveFiles")]
        public bool MoveFiles
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Move files and dirs (delete from source after copying).")]
        [DisplayName("MoveFilesNDirs")]
        [ChoPropertyInfo("moveFilesNDirs")]
        public bool MoveFilesNDirs
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Add the given attributes to copied files.")]
        [DisplayName("AddFileAttributes")]
        [ChoPropertyInfo("addFileAttributes")]
        [Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        public string AddFileAttributes
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Remove the given Attributes from copied files.")]
        [DisplayName("RemoveFileAttributes")]
        [ChoPropertyInfo("removeFileAttributes")]
        [Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        public string RemoveFileAttributes
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Create directory tree and zero-length files only.")]
        [DisplayName("CreateDirTree")]
        [ChoPropertyInfo("createDirTree")]
        public bool CreateDirTree
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Create destination files using 8.3 FAT file names only.")]
        [DisplayName("CreateFATFileNames")]
        [ChoPropertyInfo("createFATFileNames")]
        public bool CreateFATFileNames
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Turn off very long path (> 256 characters) support.")]
        [DisplayName("TurnOffLongPath")]
        [ChoPropertyInfo("turnOffLongPath")]
        public bool TurnOffLongPath
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Monitor source; run again when more than n changes seen.")]
        [DisplayName("MonitorNRunAgainChanges")]
        [ChoPropertyInfo("monitorNRunAgainChanges")]
        public uint MonitorNRunAgainChanges
        {
            get;
            set;
        }

        [Category("Copy Options")]
        [Description("Monitor source; run again in m minutes time, if changed.")]
        [DisplayName("MonitorNRunAgainChanges")]
        [ChoPropertyInfo("monitorNRunAgainChanges")]
        public uint MonitorNRunAgainChanges1
        {
            get;
            set;
        }

        #endregion Instance Data Members (Copy Options)

        public void Reset()
        {
            ChoObject.ResetObject(this);
            Persist();
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
