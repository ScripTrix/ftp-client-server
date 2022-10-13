using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client.Model
{
    public class ServerResponse : BindableBase
    {
        private string _code;
        private string _prompt;

        public string Code
        {
            get { return _code; }
            private set { _code = value; RaisePropertyChanged("Code"); }
        }

        public string Prompt
        {
            get { return _prompt; }
            private set { _prompt = value; RaisePropertyChanged("Prompt"); }
        }

        public ServerResponse(string response)
        {
            response.Trim(new char[] { '\r', '\n' });
            var splits = response.Split(" ");
            Code = (splits.Length > 0) ? splits[0] : "None";
            Prompt = (splits.Length > 1) ? string.Join(" ", splits.Skip(1)) : "None";
        }
    }
}
