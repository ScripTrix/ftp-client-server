using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace Client.Model
{
    public class FtpClient : BindableBase
    {
        private SessionInfo _sessionInfo;
        private CommandLogger _commandLogger;
        private TcpClient _commandTransfer;
        private TcpClient _dataTransfer;

        public TcpClient CommandTransfer
        {
            get { return _commandTransfer; }
            private set { _commandTransfer = value; RaisePropertyChanged("CommandTransfer"); }
        }

        public TcpClient DataTransfer
        {
            get { return _dataTransfer; }
            private set { _dataTransfer = value; RaisePropertyChanged("DataTransfer"); }
        }

        public CommandLogger CommandLogger
        {
            get { return _commandLogger; }
            set { _commandLogger = value; RaisePropertyChanged("CommandLogger"); }
        }

        public SessionInfo SessionInfo
        {
            get { return _sessionInfo; }
            private set { _sessionInfo = value; RaisePropertyChanged("SessionInfo"); }
        }

        public FtpClient(SessionInfo sessionInfo)
        {
            SessionInfo = sessionInfo;
            CommandLogger = new CommandLogger();
            CommandTransfer = new TcpClient();
        }

        public bool ConnectToServer()
        {
            try
            {
                CommandTransfer.Connect(SessionInfo.Host, SessionInfo.Port);
                ReceiveMessage();
                return Authenticate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something wrong with connection to server: {ex}");
                return false;
            }
        }

        private bool Authenticate()
        {
            var output = false;
            try
            {
                var msg = $"{Commands.USER} {SessionInfo.Username}\r\n";
                SendMessage(msg);
                var result = new ServerResponse(ReceiveMessage());
                if (!string.IsNullOrEmpty(result.Code))
                {
                    if (result.Code[0] != '3')
                    {
                        return output;
                    }
                    msg = $"{Commands.PASS} {SessionInfo.Password}\r\n";
                    SendMessage(msg);
                    result = new ServerResponse(ReceiveMessage());
                    if (!string.IsNullOrEmpty(result.Code))
                    {
                        if (result.Code[0] != '2')
                        {
                            return output;
                        }
                        output = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something wrong with client authenticate: {ex}");
                output = false;
            }
            return output;
        }

        private void SendMessage(string msg)
        {
            if (CommandTransfer.Client.Connected)
            {
                byte[] byteData = Encoding.UTF8.GetBytes(msg);
                var bytesSent = Convert.ToInt64(CommandTransfer.Client.Send(byteData));
                CommandLogger.AddNewCommand("Client", msg.Trim(new char[] { '\r', '\n' }));
                CommandLogger.AddNewCommand("Application", $"{bytesSent} bytes sent to server.");
            }
        }

        private string ReceiveMessage()
        {
            try
            {
                var currByte = new Byte[1];
                var buffer = new StringBuilder();
                while (CommandTransfer.Client.Connected && currByte[0] != ((byte)'\n'))
                {
                    currByte = new Byte[1];
                    var byteCounter = CommandTransfer.Client.Receive(currByte, currByte.Length, SocketFlags.None);
                    if (byteCounter.Equals(1))
                    {
                        buffer.Append(Encoding.UTF8.GetString(currByte));
                    }
                }
                var output = buffer.ToString().Trim(new char[] { '\r', '\n' });
                CommandLogger.AddNewCommand("Server", output);
                return output;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something wrong with receiving message: {ex}");
                return "";
            }
        }

        private bool ConnectDataTransfer(string host,int port)
        {
            DataTransfer = new TcpClient(host, port);
            if (DataTransfer.Connected)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void DisconnectDataTransfer()
        {
            if(DataTransfer == null)
            {
                return;
            }
            if (DataTransfer.Connected)
            {
                try
                {
                    DataTransfer.Close();
                }
                catch { }
            }
            DataTransfer = null;
        }

        private byte[] ReceiveData()
        {
            if (DataTransfer.Connected && CommandTransfer.Available == 0)
            {
                byte[] curr = null;
                
                NetworkStream stream = DataTransfer.GetStream();
                while (DataTransfer.Connected)
                {
                    byte[] fileSizeBytes = new byte[4];
                    int bytes = stream.Read(fileSizeBytes, 0, 4);
                    int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);

                    int bytesLeft = dataLength;

                    curr = new byte[dataLength];

                    int bufferSize = 8192;
                    int bytesRead = 0;

                    while (bytesLeft > 0)
                    {
                        int curDataSize = Math.Min(bufferSize, bytesLeft);
                        if (DataTransfer.Available < curDataSize)
                            curDataSize = DataTransfer.Available;

                        bytes = stream.Read(curr, bytesRead, curDataSize);

                        bytesRead += curDataSize;
                        bytesLeft -= curDataSize;
                    }
                    return curr;
                }
            }
            return null;
        }

        private void SendData(string data)
        {
            if (DataTransfer.Connected)
            {
                using (var ns = new NetworkStream(DataTransfer.Client))
                {
                    int bufferSize = 8192;
                    byte[] byteData = Encoding.UTF8.GetBytes(data);
                    byte[] dataSize = BitConverter.GetBytes(byteData.Length);

                    ns.Write(dataSize, 0, 4);

                    int bytesSent = 0;
                    int bytesLeft = data.Length;

                    while (bytesLeft > 0)
                    {
                        int curDataSize = Math.Min(bufferSize, bytesLeft);

                        ns.Write(byteData, bytesSent, curDataSize);

                        bytesSent += curDataSize;
                        bytesLeft -= curDataSize;
                    }
                }
            }
        }

        public string GetFileObjectInfo()
        {
            var output = "";
            var msg = $"{Commands.EPSV}\r\n";
            SendMessage(msg);
            var response = new ServerResponse(ReceiveMessage());
            if (!string.IsNullOrEmpty(response.Code))
            {
                if(response.Code[0] != '2')
                {
                    return output;
                }
                var port = int.Parse(Regex.Match(response.Prompt, @"\(\|\|\|\d+\|\)").Value.Replace("(|||", "").Replace("|)", ""));
                var connRes = ConnectDataTransfer(SessionInfo.Host, port);
                if (connRes)
                {
                    msg = $"{Commands.LIST}\r\n";
                    SendMessage(msg);
                    response = new ServerResponse(ReceiveMessage());
                    if (!string.IsNullOrEmpty(response.Code))
                    {
                        if (response.Code[0] != '1')
                        {
                            DisconnectDataTransfer();
                            return "";
                        }
                        output = Encoding.UTF8.GetString(ReceiveData());
                        SendMessage($"{Commands.NOOP}\r\n");
                        response = new ServerResponse(ReceiveMessage());
                        if (!string.IsNullOrEmpty(response.Code))
                        {
                            if (response.Code[0] == '2')
                            {
                                DisconnectDataTransfer();
                                return output;
                            }
                        }
                    }
                }
            }
            return "";
        }

        public byte[] DownloadFile(string path)
        {
            byte[] output = null;
            var msg = $"{Commands.EPSV}\r\n";
            SendMessage(msg);
            var response = new ServerResponse(ReceiveMessage());
            if (!string.IsNullOrEmpty(response.Code))
            {
                if (response.Code[0] != '2')
                {
                    return null;
                }
                var port = int.Parse(Regex.Match(response.Prompt, @"\(\|\|\|\d+\|\)").Value.Replace("(|||", "").Replace("|)", ""));
                var connRes = ConnectDataTransfer(SessionInfo.Host, port);
                if (connRes)
                {
                    msg = $"{Commands.STOR} {path}\r\n";
                    SendMessage(msg);
                    response = new ServerResponse(ReceiveMessage());
                    if (!string.IsNullOrEmpty(response.Code))
                    {
                        if (response.Code[0] != '1')
                        {
                            DisconnectDataTransfer();
                            return null;
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        output = ReceiveData();
                        SendMessage($"{Commands.NOOP}\r\n");
                        response = new ServerResponse(ReceiveMessage());
                        if (!string.IsNullOrEmpty(response.Code))
                        {
                            if (response.Code[0] == '2')
                            {
                                DisconnectDataTransfer();
                                return output;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public bool UploadFile(string data)
        {
            var output = false;

            return output;
        }

        public string ChangeCurrentDirectory(string path)
        {
            var output = "";
            var msg = $"{Commands.CWD} {path}\r\n";
            SendMessage(msg);
            var response = new ServerResponse(ReceiveMessage());
            if (!string.IsNullOrEmpty(response.Code))
            {
                if(response.Code[0] == '2')
                {
                    msg = $"{Commands.PWD}\r\n";
                    SendMessage(msg);
                    response = new ServerResponse(ReceiveMessage());
                    if (!string.IsNullOrEmpty(response.Code))
                    {
                        if (response.Prompt.Contains(path))
                        {
                            if (response.Code[0] == '2')
                            {
                                output = GetFileObjectInfo();
                                return output;
                            }
                        }
                    }
                }
            }
            return "";
        }
    }
}
