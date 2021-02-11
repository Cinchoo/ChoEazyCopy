namespace ChoEazyCopy
{
    #region NameSpaces

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Windows;
    using Cinchoo.Core.IO;
    using Cinchoo.Core;
    using System.Threading;
    using System.Diagnostics;
    using System.Threading.Tasks;

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

        private Process _process = null;

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

        public void Process(string fileName, string arguments, string preCommands, string postCommands, bool console = false)
        {
            AppStatus.Raise(this, new ChoFileProcessEventArgs("Starting RoboCopy operation..."));
            Status.Raise(this, new ChoFileProcessEventArgs(Environment.NewLine));

            try
            {
                // Setup the process start info
                var processStartInfo = new ProcessStartInfo("cmd.exe", " /K /E:OFF /F:OFF /V:OFF") // new ProcessStartInfo(fileName, arguments) //_appSettings.RoboCopyFilePath, _appSettings.GetCmdLineParams(sourceDirectory, destDirectory))
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Setup the process
                Process process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };

                // Register event
                _process = process;

                // Start process
                process.Start();

                //process.BeginOutputReadLine();
                Task.Factory.StartNew(new Action<object>(ReadFromStreamReader), process.StandardOutput);
                //Task.Factory.StartNew(new Action<object>(ReadFromStreamReader), process.StandardError);

                if (!console)
                    process.StandardInput.WriteLine("prompt $G");

                //Run precommands
                if (!preCommands.IsNullOrWhiteSpace())
                {
                    foreach (var cmd in preCommands.SplitNTrim().Select(c => c.NTrim()).Where(c => !c.IsNullOrWhiteSpace()))
                        process.StandardInput.WriteLine($"{cmd}");
                }
                process.StandardInput.WriteLine($"{fileName} {arguments}");

                //Run postcommands
                if (!postCommands.IsNullOrWhiteSpace())
                {
                    foreach (var cmd in postCommands.SplitNTrim().Select(c => c.NTrim()).Where(c => !c.IsNullOrWhiteSpace()))
                        process.StandardInput.WriteLine($"{cmd}");
                }
                process.StandardInput.WriteLine("exit");

                process.WaitForExit();

                _process = null;
                AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation completed successfully.", "RoboCopy operation completed successfully"));
            }
            catch (ThreadAbortException)
            {
                Status.Raise(this, new ChoFileProcessEventArgs(Environment.NewLine + "RoboCopy operation canceled by user." + Environment.NewLine, "RoboCopy operation failed."));
                AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation canceled by user.", "RoboCopy operation failed."));
            }
            catch (Exception ex)
            {
                Status.Raise(this, new ChoFileProcessEventArgs(Environment.NewLine + ex.ToString() + Environment.NewLine));
                AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation failed.", "RoboCopy operation failed."));
            }
        }

        bool cleanup = false;
        private string CleanUp(string txt)
        {
            //if (!cleanup)
            //{
            //    if (txt.Contains(Environment.NewLine))
            //        txt = txt.Substring(txt.IndexOf(Environment.NewLine));
            //    else
            //        txt = null;

            //    cleanup = true;
            //}

            return txt;
        }

        void ReadFromStreamReader(object state)
        {
            cleanup = false;
            StreamReader reader = state as StreamReader;
            char[] buffer = new char[1024];
            int chars;
            StringBuilder txt = new StringBuilder();
            while ((chars = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                string data = new string(buffer, 0, chars);
                txt.Append(data);

                if (txt.Length > 0)
                { 
                    Status.Raise(this, new ChoFileProcessEventArgs(CleanUp(txt.ToString())));
                    txt.Clear();
                }

                Thread.Sleep(30);
            }
            if (txt.Length > 0)
            {
                Status.Raise(this, new ChoFileProcessEventArgs(CleanUp(txt.ToString())));
                txt.Clear();
            }

            // You arrive here when process is terminated.
        }

        internal void Cancel()
        {
            Process process = _process;
            if (process == null) return;

            try
            {
                process.Kill();
                AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation canceled."));
                _process = null;
            }
            catch { }
        }

        #endregion Instance Members (Public)

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}
