using System;
using System.Collections.Generic;
using System.Text;

namespace Server.FtpServer.DTO
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public Operations AvailableOperations { get; set; }

        public string Hash { get; set; }

        public string Salt { get; set; }
    }
}
