using System;
using System.Collections.Generic;
using System.Text;

namespace Server.FtpServer.DTO
{
    public enum Operations
    { 
        NONE = 0,
        R,      // Read only
        W,      // Write only
        RW      // Read and Write
    }
}
