using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoEazyCopy
{
    public class ChoBackupTaskInfo
    {
        public string TaskName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public ChoBackupTaskInfo(string filePath)
        {
            var fi = new FileInfo(filePath);

            TaskName = Path.GetFileNameWithoutExtension(filePath);
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            CreatedDate = fi.CreationTime;
            ModifiedDate = fi.LastWriteTime;
        }
    }

}
