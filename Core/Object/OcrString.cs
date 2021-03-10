using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dofus.Retro.Supertools.Core.Object
{
    public class OcrString
    {
        public string Name { get; set; }
        public string Data { get; set; }

        public OcrString(string name, string data)
        {
            Name = name;
            Data = data;
        }
    }
}
