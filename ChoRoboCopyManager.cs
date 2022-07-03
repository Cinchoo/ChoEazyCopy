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
    using System.Management;

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
    public class ChoRoboCopyProgressEventArgs : EventArgs
    {
        #region Instance Data Members (Public)

        public long _runningBytes
        {
            get;
            private set;
        }

        public long _runningFileCount
        {
            get;
            private set;
        }

        public long _totalBytes
        {
            get;
            private set;
        }

        public long _totalFileCount
        {
            get;
            private set;
        }

        #endregion Instance Data Members (Public)

        #region Constructors

        public ChoRoboCopyProgressEventArgs(long runningBytes, long runningFileCount, long totalBytes, long totalFileCount)
        {
            _runningBytes = runningBytes;
            _runningFileCount = runningFileCount;
            _totalBytes = totalBytes;
            _totalFileCount = totalFileCount;
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
        public event EventHandler<ChoRoboCopyProgressEventArgs> Progress;

        #endregion EventHandlers

        #region Instance Data Members (Private)

        private Process _process = null;
        private Process _robocopyProcess = null;
        private Process _analyzeRobocopyProcess = null;

        private long _totalFileCount = 0;
        private long _totalBytes = 0;
        private long _runningFileCount = 0;
        private long _runningBytes = 0;
        private bool _cancel = false;
        private bool _hasError = false;

        AutoResetEvent _waitForRobocopyProcessToExit = new AutoResetEvent(false);
        private Regex _regexBytes = new Regex(@"(?<=\s+)\d+(?=\s+)", RegexOptions.Compiled);

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

        public void Process(ChoAppSettings appSettings, bool console = false)
        {
            string fileName = appSettings.RoboCopyFilePath;
            string arguments = appSettings.GetCmdLineParams();

            AppStatus.Raise(this, new ChoFileProcessEventArgs("Starting RoboCopy operation..."));
            Status.Raise(this, new ChoFileProcessEventArgs(Environment.NewLine));

            string preCommands = appSettings.Precommands;
            string postCommands = appSettings.Postcommands;
            bool testRun = appSettings.ListOnly;
            _cancel = false;
            
            try
            {
                // Setup the process start info
                var processStartInfo = new ProcessStartInfo("cmd.exe", " /E:OFF /F:OFF /V:OFF /K") // new ProcessStartInfo(fileName, arguments) //_appSettings.RoboCopyFilePath, _appSettings.GetCmdLineParams(sourceDirectory, destDirectory))
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

                string echoCmd = testRun ? "@ECHO " : "";
                //Run precommands
                    
                Status.Raise(this, new ChoFileProcessEventArgs($"**************************************" + Environment.NewLine));
                Status.Raise(this, new ChoFileProcessEventArgs($"Starting RoboCopy operations..." + Environment.NewLine));

                if (!preCommands.IsNullOrWhiteSpace())
                {
                    _hasError = false;

                    // Start process
                    process.Start();

                    //Task.Factory.StartNew(new Action<object>(ReadFromStreamReader), process.StandardOutput);
                    Task.Factory.StartNew(new Action<object>(ReadFromStreamReader), process.StandardError);

                    //Replace tokens
                    preCommands = preCommands.Replace("{SRC_DIR}", appSettings.SourceDirectory);
                    preCommands = preCommands.Replace("{DEST_DIR}", appSettings.DestDirectory);

                    if (!testRun)
                        Status.Raise(this, new ChoFileProcessEventArgs($"Executing pre-process commands..." + Environment.NewLine));
                    else
                        Status.Raise(this, new ChoFileProcessEventArgs($"SKIP: Executing pre-process commands..." + Environment.NewLine));

                    Status.Raise(this, new ChoFileProcessEventArgs($"{FormatCmd(preCommands, appSettings)}" + Environment.NewLine));
                    if (!testRun)
                        process.StandardInput.WriteLine(preCommands);

                    //foreach (var cmd in preCommands.SplitNTrim().Select(c => c.NTrim()).Select(c => MarshalCmd(c, appSettings)).Where(c => !c.IsNullOrWhiteSpace()))
                    //{
                    //    Status.Raise(this, new ChoFileProcessEventArgs($">{echoCmd}{cmd}" + Environment.NewLine));
                    //    process.StandardInput.WriteLine($"{echoCmd}{cmd}");
                    //}

                    process.StandardInput.WriteLine("exit");
                    process.WaitForExit();

                    if (_hasError)
                    {
                        Status.Raise(this, new ChoFileProcessEventArgs($"RoboCopy operations failed." + Environment.NewLine));
                        AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operations failed.", "RoboCopy operations failed."));
                        return;
                    }
                }

                //Run robocopy
                //process.StandardInput.WriteLine($"{fileName} {arguments}");

                if (appSettings.ShowRoboCopyProgress)
                {
                    if (!Analyze(appSettings))
                        return;
                }
                if (!RunRoboCopyOperation(appSettings))
                    return;

                //Run postcommands
                if (!postCommands.IsNullOrWhiteSpace())
                {
                    _hasError = false;

                    // Start process
                    process.Start();

                    //Task.Factory.StartNew(new Action<object>(ReadFromStreamReader), process.StandardOutput);
                    Task.Factory.StartNew(new Action<object>(ReadFromStreamReader), process.StandardError);

                    //Replace tokens
                    postCommands = postCommands.Replace("{SRC_DIR}", appSettings.SourceDirectory);
                    postCommands = postCommands.Replace("{DEST_DIR}", appSettings.DestDirectory);

                    if (!testRun)
                        Status.Raise(this, new ChoFileProcessEventArgs($"Executing post-process commands..." + Environment.NewLine));
                    else
                        Status.Raise(this, new ChoFileProcessEventArgs($"SKIP: Executing post-process commands..." + Environment.NewLine));

                    Status.Raise(this, new ChoFileProcessEventArgs($"{FormatCmd(postCommands, appSettings)}" + Environment.NewLine));
                    if (!testRun)
                        process.StandardInput.WriteLine(postCommands);

                    //foreach (var cmd in postCommands.SplitNTrim().Select(c => c.NTrim()).Select(c => MarshalCmd(c, appSettings)).Where(c => !c.IsNullOrWhiteSpace()))
                    //{
                    //    Status.Raise(this, new ChoFileProcessEventArgs($">{echoCmd}{cmd}" + Environment.NewLine));
                    //    process.StandardInput.WriteLine($"{echoCmd}{cmd}");
                    //}
                    process.StandardInput.WriteLine("exit");
                    process.WaitForExit();

                    if (_hasError)
                    {
                        Status.Raise(this, new ChoFileProcessEventArgs($"RoboCopy operations failed." + Environment.NewLine));
                        AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operations failed.", "RoboCopy operations failed."));
                        return;
                    }
                }

                _process = null;

                if (!_cancel)
                {
                    Status.Raise(this, new ChoFileProcessEventArgs($"RoboCopy operations completed successfully." + Environment.NewLine));
                    AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation completed successfully.", "RoboCopy operation completed successfully."));
                }
                else
                {
                    Status.Raise(this, new ChoFileProcessEventArgs($"RoboCopy operations cancelled by user." + Environment.NewLine));
                    AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operations cancelled by user.", "RoboCopy operations cancelled by user."));
                }
            }
            catch (ThreadAbortException)
            {
                Status.Raise(this, new ChoFileProcessEventArgs(Environment.NewLine + "RoboCopy operation canceled by user." + Environment.NewLine, "RoboCopy operation canceled by user."));
                AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation canceled by user.", "RoboCopy operation canceled by user."));
            }
            catch (Exception ex)
            {
                Status.Raise(this, new ChoFileProcessEventArgs(Environment.NewLine + ex.ToString() + Environment.NewLine));
                AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation failed.", "RoboCopy operation failed."));
            }
        }

        private string FormatCmd(string cmd, ChoAppSettings appSettings)
        {
            return cmd.Split("\n").Select(line => $">{line.Trim()}").Join(Environment.NewLine);
        }

        public bool RunRoboCopyOperation(ChoAppSettings appSettings)
        {
            string fileName = appSettings.RoboCopyFilePath;
            string arguments = appSettings.GetCmdLineParams();

            AppStatus.Raise(this, new ChoFileProcessEventArgs("Performing RoboCopy operation..."));
            Status.Raise(this, new ChoFileProcessEventArgs("Performing RoboCopy operation..."));

            string preCommands = appSettings.Precommands;
            string postCommands = appSettings.Postcommands;
            bool testRun = appSettings.ListOnly;

            try
            {
                _waitForRobocopyProcessToExit.Reset();

                // Setup the process start info
                var processStartInfo = new ProcessStartInfo(fileName, arguments) // new ProcessStartInfo(fileName, arguments) //_appSettings.RoboCopyFilePath, _appSettings.GetCmdLineParams(sourceDirectory, destDirectory))
                {
                    UseShellExecute = false,
                    //RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                Status.Raise(this, new ChoFileProcessEventArgs(Environment.NewLine));

                // Setup the process
                Process process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };
                Status.Raise(this, new ChoFileProcessEventArgs($">{fileName} {arguments}"));

                // Register event
                _robocopyProcess = process;

                // Start process
                process.Start();

                //process.BeginOutputReadLine();
                Task.Factory.StartNew(new Action<object>(ParseRobocopyOutput), new Tuple<StreamReader, bool>(process.StandardOutput, false));
                Task.Factory.StartNew(new Action<object>(ReadFromStreamReader), process.StandardError);

                //Run robocopy
                //process.StandardInput.WriteLine($"{fileName} {arguments} /L");

                //process.StandardInput.WriteLine("exit");

                process.WaitForExit();
                _waitForRobocopyProcessToExit.WaitOne();
                Status.Raise(this, new ChoFileProcessEventArgs(Environment.NewLine));

                _robocopyProcess = null;
            }
            catch (ThreadAbortException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool Analyze(ChoAppSettings appSettings)
        {
            string fileName = appSettings.RoboCopyFilePath;
            string arguments = appSettings.GetCmdLineParams() + " /L";

            AppStatus.Raise(this, new ChoFileProcessEventArgs("Analyzing RoboCopy operation..."));
            Status.Raise(this, new ChoFileProcessEventArgs("Analyzing RoboCopy operation..."));

            string preCommands = appSettings.Precommands;
            string postCommands = appSettings.Postcommands;
            bool testRun = appSettings.ListOnly;

            try
            {
                _waitForRobocopyProcessToExit.Reset();

                // Setup the process start info
                var processStartInfo = new ProcessStartInfo(fileName, arguments) // new ProcessStartInfo(fileName, arguments) //_appSettings.RoboCopyFilePath, _appSettings.GetCmdLineParams(sourceDirectory, destDirectory))
                {
                    UseShellExecute = false,
                    //RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Setup the process
                Process process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };

                // Register event
                _analyzeRobocopyProcess = process;

                // Start process
                process.Start();

                //process.BeginOutputReadLine();
                Task.Factory.StartNew(new Action<object>(ParseRobocopyOutput), new Tuple<StreamReader, bool>(process.StandardOutput, true));
                Task.Factory.StartNew(new Action<object>(ReadFromStreamReader), process.StandardError);

                //Run robocopy
                //process.StandardInput.WriteLine($"{fileName} {arguments} /L");

                //process.StandardInput.WriteLine("exit");

                process.WaitForExit();
                _waitForRobocopyProcessToExit.WaitOne();

                _analyzeRobocopyProcess = null;
            }
            catch (ThreadAbortException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
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

        private void ParseRobocopyOutput(object state)
        {
            cleanup = false;
            
            StreamReader reader = ((Tuple<StreamReader, bool>)state).Item1;
            bool isAnalyze = ((Tuple<StreamReader, bool>)state).Item2;

            char[] buffer = new char[32768];
            int chars;
            StringBuilder txt = new StringBuilder();
            while ((chars = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                string data = new string(buffer, 0, chars);
                txt.Append(data);

                if (txt.Length > 0)
                {
                    var msg = CleanUp(txt.ToString());
                    int pos = msg.LastIndexOf(Environment.NewLine);

                    if (pos >= 0)
                    {
                        ParseRobocopyOutputData(msg.Substring(0, pos), isAnalyze); 
                        txt.Clear();
                        txt.Append(msg.Substring(pos + 1));
                    }
                }
            }
            if (txt.Length > 0)
            {
                ParseRobocopyOutputData(CleanUp(txt.ToString()), isAnalyze);
                txt.Clear();
            }
            _waitForRobocopyProcessToExit.Set();
            // You arrive here when process is terminated.
        }

        private void ParseRobocopyOutputData(string msg, bool isAnalyze)
        {
            if (!isAnalyze && msg.Length > 0)
                Status.Raise(this, new ChoFileProcessEventArgs(msg));

            var lines = msg.Split(Environment.NewLine).Where(m => !m.IsNullOrWhiteSpace()).ToArray();

            long fileSize = 0;
            foreach (var line in lines)
            {
                fileSize = 0;
                var match = _regexBytes.Match(line);
                if (match.Success)
                {
                    fileSize += match.Value.CastTo<long>();
                }

                if (isAnalyze)
                {
                    _totalBytes += fileSize;
                    _totalFileCount++;
                }
                else
                {
                    _runningBytes += fileSize;
                    _runningFileCount++;
#if TEST_MODE
                    Thread.Sleep(1000);
#endif
                    var progress = Progress;
                    if (progress != null && _totalBytes > 0)
                    {
                        progress.Raise(null, new ChoRoboCopyProgressEventArgs(_runningBytes, _runningFileCount, _totalBytes, _totalFileCount));
                    }
                }
            }
        }

        private void ReadFromStreamReader(object state)
        {
            cleanup = false;
            StreamReader reader = state as StreamReader;
            char[] buffer = new char[32768];
            int chars;
            StringBuilder txt = new StringBuilder();
            while ((chars = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                string data = new string(buffer, 0, chars);
                txt.Append(data);

                if (txt.Length > 0)
                {
                    _hasError = true;
                    Status.Raise(this, new ChoFileProcessEventArgs(CleanUp(txt.ToString())));
                    txt.Clear();
                }
            }
            if (txt.Length > 0)
            {
                _hasError = true;
                Status.Raise(this, new ChoFileProcessEventArgs(CleanUp(txt.ToString())));
                txt.Clear();
            }

            // You arrive here when process is terminated.
        }

        internal void Cancel()
        {
            _cancel = true;
            _waitForRobocopyProcessToExit.Set();

            Process process = _analyzeRobocopyProcess;
            if (process != null)
            {
                try
                {
                    try
                    {
                        KillProcessAndChildrens(_process.Id);
                    }
                    catch { }

                    process.Kill();
                    //AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation canceled."));
                    _analyzeRobocopyProcess = null;
                }
                catch { }
            }
            process = _robocopyProcess;
            if (process != null)
            {
                try
                {
                    try
                    {
                        KillProcessAndChildrens(_process.Id);
                    }
                    catch { }

                    process.Kill();
                    //AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation canceled."));
                    _robocopyProcess = null;
                }
                catch { }
            }

            process = _process;
            if (process != null)
            {
                try
                {
                    try
                    {
                        KillProcessAndChildrens(_process.Id);
                    }
                    catch { }

                    process.Kill();
                    AppStatus.Raise(this, new ChoFileProcessEventArgs("RoboCopy operation canceled."));
                    _process = null;
                }
                catch { }
            }
        }

        private void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            // We must kill child processes first!
            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }

            // Then kill parents.
            try
            {
                Process proc = System.Diagnostics.Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
        #endregion Instance Members (Public)

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}
