using Cinchoo.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoEazyCopy
{
    public static class ChoTaskQueueItemLogInfo
    {
        public static string AppLogFolder
        {
            get;
            private set;
        }

        static ChoTaskQueueItemLogInfo()
        {
            AppLogFolder = Path.Combine(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                ChoGlobalApplicationSettings.Me.ApplicationNameWithoutExtension), "Log");
            Directory.CreateDirectory(AppLogFolder);
        }
    }
}
