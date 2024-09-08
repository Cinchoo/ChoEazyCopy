using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoEazyCopy
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
    public class ChoAssemblyBetaVersionAttribute : Attribute
    {
        public string Version { get; set; }
        public ChoAssemblyBetaVersionAttribute(string text)
        {
            Version = text;
        }
    }

}
