using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Model
{
    public class CommandFormatModel : BindableBase
    {
        private string _host;
        private string _message;

        public string Host
        {
            get { return _host; }
            set { _host = value; RaisePropertyChanged("Host"); }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; RaisePropertyChanged("Message"); }
        }

        public CommandFormatModel(string host, string msg)
        {
            Host = host;
            Message = msg;
        }
    }
}
