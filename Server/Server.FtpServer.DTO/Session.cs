using Server.BLL;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Server.FtpServer.DTO
{
    public class Session
    {
        public int Id { get; set; }

        public User User { get; set; }

        public DateTime Connected { get; set; }

        public bool IsAuthenticate { get; set; }

        public DateTime Disconnected { get; set; }

        public DirectoryEngine DirectoryEngine { get; set; }

        public Socket CommandTransfer { get; set; }

        public string CurrentCommand { get; set; }

        public string PreviousCommand { get; set; }
        
        public Socket DataTransfer { get; set; }
        
        public FtpModes Mode { get; set; }

        public FtpTypes Type { get; set; }

        public Dictionary<Commands, Delegate> AvailableCommands { get; set; } = new Dictionary<Commands, Delegate>();

        public static List<User> Users { get; set; } = new List<User>();
    }
}
