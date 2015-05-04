namespace ChoEazyCopy
{
    #region NameSpaces

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Windows;
    using Cinchoo.Core.IO;
    using Cinchoo.Core;
    using System.Threading;
    using System.Diagnostics;

    #endregion NameSpaces

    public class ChoFileProcessEventArgs : EventArgs
    {
        #region Instance Data Members (Public)

        public string Message
        {
            get;
            private set;
        }

        public object Tag
        {
            get;
            private set;
        }

        #endregion Instance Data Members (Public)

        #region Constructors

        public ChoFileProcessEventArgs(string message, object tag = null)
        {
            Message = message;
            Tag = tag;
        }

        #endregion Constructors
    }

    public class ChoRoboCopyManager : IDisposable
    {
        #region Shared Data Members (Private)

        private static readonly ChoAppSettings _appSettings = new ChoAppSettings();

        #endregion Shared Data Members (Private)

        #region EventHandlers

        public event EventHandler<ChoFileProcessEventArgs> Status;
        public event EventHandler<ChoFileProcessEventArgs> AppStatus;

        #endregion EventHandlers

        #region Instance Data Members (Private)

        private bool _cancelRequested = false;

        #endregion Instance Data Members (Private)

        #region Constructors

        public ChoRoboCopyManager(string settingsFilePath = null)
        {
            if (settingsFilePath.IsNullOrWhiteSpace())
            {
                if (File.Exists(settingsFilePath))
                {
                    string settingsText = File.ReadAllText(settingsFilePath);
                    _appSettings.LoadXml(settingsText);
                }
            }
        }

        #endregion Constructors

        #region Instance Members (Public)

        public void Process()
        {
            AppStatus.Raise(this, new ChoFileProcessEventArgs("Starting RoboCopy operation...", "Busy"));

            // Setup the process start info
            var processStartInfo = new ProcessStartInfo(_appSettings.RoboCopyFilePath, _appSettings.GetCmdLineParams())
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            // Setup the process
            Process process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };

            // Register event
            process.OutputDataReceived += OnOutputDataReceived;

            // Start process
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            process.Kill();
            AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation completed successfully.", "Ready"));
        }

        void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Status.Raise(this, new ChoFileProcessEventArgs(e.Data));
        }

        #endregion Instance Members (Public)

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        internal void Cancel()
        {
            throw new NotImplementedException();
        }
    }
}
