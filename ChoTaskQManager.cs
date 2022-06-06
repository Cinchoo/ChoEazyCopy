using Cinchoo.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChoEazyCopy
{
    internal class ChoTaskQManager
    {
        private readonly ICollection<ChoTaskQueueItem> _taskQItems;
        private readonly object _padLock;
        private Thread _roboCopyThread;

        public ChoTaskQManager(ICollection<ChoTaskQueueItem> taskQItems, object padLock)
        {
            _taskQItems = taskQItems;
            _padLock = padLock;
        }

        public void Start()
        {
#if TEST_MODE
            return;
#endif
            if (_roboCopyThread != null)
                return;
            
            ChoTaskQueueItem firstTaskQueueItem = null;

            _roboCopyThread = new Thread(() =>
            {
                int count = 0;
                while (true)
                {
                    lock (_padLock)
                    {
                        count = _taskQItems.Count;
                        if (count > 0)
                        {
                            firstTaskQueueItem = GetFirstTaskQueueItem();
                            if (firstTaskQueueItem != null)
                            {
                                RunRoboCopyOperation(firstTaskQueueItem);
                            }
                        }
                    }

                    if (firstTaskQueueItem == null)
                        Thread.Sleep(10 * 1000);
                }
            });
            _roboCopyThread.Start();
        }

        private void RunRoboCopyOperation(ChoTaskQueueItem taskQueueItem)
        {
            try
            {
                taskQueueItem.StartTime = DateTime.Now;
                taskQueueItem.ErrorMessage = null;

                if (taskQueueItem.TaskFilePath.IsNullOrWhiteSpace())
                    throw new ApplicationException("Missing task file path.");
                if (taskQueueItem.TaskFilePath.IsNullOrWhiteSpace())
                    throw new ApplicationException($"'{taskQueueItem.TaskFilePath}' task file path does not exists.");

                ChoAppSettings appSettings = new ChoAppSettings();
                appSettings.LoadXml(File.ReadAllText(taskQueueItem.TaskFilePath));

                using (var log = new StreamWriter(taskQueueItem.LogFilePath))
                {
                    ChoRoboCopyManager _roboCopyManager = new ChoRoboCopyManager();
                    _roboCopyManager.Status += (sender, e) => log.Write(e.Message);
                    //_roboCopyManager.AppStatus += (sender, e) => UpdateStatus(e.Message, e.Tag.ToNString());

                    _roboCopyManager.Process(appSettings.RoboCopyFilePath, appSettings.GetCmdLineParams(), appSettings);
                }

                taskQueueItem.Status = TaskStatus.Completed;
            }
            catch (ThreadAbortException)
            {
                taskQueueItem.Status = TaskStatus.Stopped;
                Thread.ResetAbort();
            }
            catch (Exception ex)
            {
                taskQueueItem.Status = TaskStatus.Stopped;
                taskQueueItem.ErrorMessage = ex.Message;
            }
            finally
            {
                taskQueueItem.EndTime = DateTime.Now;
            }
        }

        private ChoTaskQueueItem GetFirstTaskQueueItem()
        {
            return _taskQItems.FirstOrDefault(t => t.Status == TaskStatus.Queued);
        }

        public void Stop()
        {
            var thread = _roboCopyThread;
            if (thread != null)
                thread.AbortThread();
        }

        public void Add(string taskName, DateTime? startTime = null, DateTime? endTime = null, string taskFilePath = null, 
            TaskStatus? status = TaskStatus.Queued, string errorMsg = null, Action<ChoTaskQueueItem> onSuccess = null, 
            Action<string, string> onFailure = null)
        {
            lock (_padLock)
            {
                try
                {
                    long index = _taskQItems.Count == 0 ? 0 : _taskQItems.Max(i => i.Id);
                    var task = new ChoTaskQueueItem(index + 1, taskName)
                    {
                        StartTime = startTime == null ? DateTime.Now : startTime.Value,
                        EndTime = endTime == null ? DateTime.Now.AddDays(1) : endTime.Value,
                        TaskFilePath = taskFilePath,
                        Status = status == null ? TaskStatus.Queued : status.Value,
                        ErrorMessage = errorMsg,
                    };

                    _taskQItems.Add(task);
                    if (onSuccess != null)
                        onSuccess(task);
                }
                catch (Exception ex)
                {
                    if (onFailure != null)
                        onFailure(taskName, ex.Message);
                }
            }
        }
    }
}
