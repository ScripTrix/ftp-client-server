using Server.FtpServer;
using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new FtpServer.FtpServer("127.0.0.1", 21);
        }
    }
}
