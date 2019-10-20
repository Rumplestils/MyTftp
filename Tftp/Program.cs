using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tftp
{
    class Program
    {
        static void Main(string[] args)
        {
            TftpServer ts = new TftpServer()
            {
                Root = @"F:\TftpPut",
                HostName = "TftpServer Example",
                HostIp = "192.168.2.10"
            };

            ts.StartTftp();
        }
    }
}
