using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCSocket.Tools.net.omron.cip
{
    class CipData
    {
        public String tagName { get; set; }
        public byte[] data { get; set; }

        public CipData()
        {
        }

        public CipData(String tagName)
        {
            this.tagName = tagName;
        }
    }
}
