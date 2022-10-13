using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Client.Model
{
    public class CommandLogger : BindableBase
    {
        private ObservableCollection<CommandFormatModel> _logs;

        public ObservableCollection<CommandFormatModel> Logs
        {
            get { return _logs; }
            private set { _logs = value; RaisePropertyChanged("Logs"); }
        }

        public CommandLogger()
        {
            Logs = new ObservableCollection<CommandFormatModel>();
            AddNewCommand("Application", "Welcome to FTP Client!");
        }

        public void AddNewCommand(string host, string msg)
        {
            Logs.Add(new CommandFormatModel(host, msg));
        }

        public void ClearCommands()
        {
            Logs.Clear();
            Logs = new ObservableCollection<CommandFormatModel>();
        }
    }
}
