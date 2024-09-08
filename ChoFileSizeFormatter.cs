using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoEazyCopy
{
    public static class ChoFileSizeFormatter
    {
        static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public static string FormatSize(Int64 bytes)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }
    }
}
