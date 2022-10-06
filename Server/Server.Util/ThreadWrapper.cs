using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Server.Util
{
    public class ThreadWrapper
    {
        public Thread Thread { get; set; }

        public CancellationTokenSource Cts { get; set; }
    }
}
