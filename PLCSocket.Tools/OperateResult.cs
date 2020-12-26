using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCSocket.Tools
{
    class OperateResult
    {
        public OperateResult()
        {
            this.success = true;
        }

        public bool success { get; set; }
        public string msg { get; set; }
        public byte[] content { get; set; }
    }
}
