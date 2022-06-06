using Cinchoo.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoEazyCopy
{
    public enum TaskStatus { Queued = 0, Running = 1, Stopped = 2, Completed = 3}

    public class ChoTaskQueueItem : ChoViewModelBase
    {
        private Guid _UID;
        public Guid UID
        {
            get { return _UID; }
            set
            {
                _UID = value;
                NotifyPropertyChanged();
            }
        }
        private long _id;
        public long Id 
        { 
            get { return _id; }
            set
            {
                _id = value;
                NotifyPropertyChanged();
            }
        }
        private string _taskName;
        public string TaskName
        {
            get { return _taskName; }
            set
            {
                _taskName = value;
                NotifyPropertyChanged();
            }
        }
        private DateTime _queueTime;
        public DateTime QueueTime
        {
            get { return _queueTime; }
            set
            {
                _queueTime = value;
                NotifyPropertyChanged();
            }
        }


        private DateTime _startTime;
        public DateTime StartTime
        {
            get { return _startTime; }
            set
            {
                _startTime = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime _endTime;
        public DateTime EndTime
        {
            get { return _endTime; }
            set
            {
                _endTime = value;
                NotifyPropertyChanged();
            }
        }

        private TaskStatus _status;
        public TaskStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged();
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                NotifyPropertyChanged();
            }
        }


        private string _taskFilePath;
        public string TaskFilePath
        {
            get { return _taskFilePath; }
            set
            {
                _taskFilePath = value;
                NotifyPropertyChanged();
            }
        }

        public string LogFilePath 
        {
            get { return Path.Combine(ChoTaskQueueItemLogInfo.AppLogFolder, $"{TaskName}_{UID}.log");  } 
        }

        public ChoTaskQueueItem()
        {

        }

        public ChoTaskQueueItem(long id, string taskName)
        {
            UID = Guid.NewGuid();
            Id = id;
            TaskName = taskName;
            QueueTime = DateTime.Now;
        }
    }

}
