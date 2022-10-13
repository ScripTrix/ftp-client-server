using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Model
{
    public class SessionInfo : BindableBase
    {
        private string _host;
        private string _username;
        private string _password;
        private int _port;

        public string Host
        {
            get { return _host; }
            set { _host = value; RaisePropertyChanged("Host"); }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; RaisePropertyChanged("Username"); }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; RaisePropertyChanged("Password"); }
        }

        public int Port
        {
            get { return _port; }
            set { _port = value; RaisePropertyChanged("Port"); }
        }

        public SessionInfo()
        {
            Host = "127.0.0.1";
            Username = "anonymous";
            Password = "anonymous@mail.ru";
            Port = 21;
        }
    }
}
