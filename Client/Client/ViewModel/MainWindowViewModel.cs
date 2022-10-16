using Client.Core;
using Client.Model;
using GalaSoft.MvvmLight.Command;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Client.ViewModel
{
    public class MainWindowViewModel : BindableBase
    {
        #region LOCAL STORAGE ViewModel
        private SessionInfo _sessionInfo;
        private ObservableCollection<FileObjectInfo> _currentDirectory;
        private ObservableCollection<FileObjectInfo> _previousDirectory;
        private ObservableCollection<FileObjectInfo> _rootDirectory;
        private FileObjectInfo _selectedFileObject;

        public FileObjectInfo SelectedFileObject
        {
            get { return _selectedFileObject; }
            set { _selectedFileObject = value; RaisePropertyChanged("SelectedFileObject"); }
        }

        public ObservableCollection<FileObjectInfo> CurrentDirectory
        {
            get { return _currentDirectory; }
            set { _currentDirectory = value; RaisePropertyChanged("CurrentDirectory"); }
        }

        public ObservableCollection<FileObjectInfo> PreviousDirectory
        {
            get { return _previousDirectory; }
            private set { _previousDirectory = value; RaisePropertyChanged("PreviousDirectory"); }
        }

        public ObservableCollection<FileObjectInfo> RootDirectory
        {
            get { return _rootDirectory; }
            set { _rootDirectory = value; RaisePropertyChanged("RootDirectory"); }
        }

        public SessionInfo SessionInfo
        {
            get { return _sessionInfo; }
            set { _sessionInfo = value; RaisePropertyChanged("SessionInfo"); }
        }

        public MainWindowViewModel()
        {
            SessionInfo = new SessionInfo();
            var drives = DriveInfo.GetDrives();
            RootDirectory = new ObservableCollection<FileObjectInfo>();
            CurrentDirectory = new ObservableCollection<FileObjectInfo>();
            drives.ToList().ForEach(drive =>
            {
                var fileObjectInfo = new FileObjectInfo(drive);
                RootDirectory.Add(fileObjectInfo);
                CurrentDirectory.Add(fileObjectInfo);
            });
        }

        private Core.RelayCommand _localDirectoryBackCommand;
        private Core.RelayCommand _localDirectoryCommand;

        public Core.RelayCommand LocalDirectoryBackCommand
        {
            get
            {
                return _localDirectoryBackCommand ??
                    (_localDirectoryBackCommand = new Core.RelayCommand(obj =>
                    {
                        LocalDirectoryBackCommandMethod();
                    }));
            }
        }

        public Core.RelayCommand LocalDirectoryCommand
        {
            get
            {
                return _localDirectoryCommand ??
                    (_localDirectoryCommand = new Core.RelayCommand(obj =>
                    {
                        LocalDirectoryCommandMethod(obj);
                    }));
            }
        }

        private void LocalDirectoryBackCommandMethod()
        {
            CurrentDirectory.Clear();
            CurrentDirectory = new ObservableCollection<FileObjectInfo>();
            RootDirectory.ToList().ForEach(node =>
            {
                CurrentDirectory.Add((FileObjectInfo)node.Clone());
            });
        }

        private void LocalDirectoryCommandMethod(object obj)
        {
            if (obj is FileObjectInfo)
            {
                if (((FileObjectInfo)obj).Type == FileObjectType.Directory || ((FileObjectInfo)obj).Type == FileObjectType.Drive)
                {
                    PreviousDirectory = new ObservableCollection<FileObjectInfo>();
                    CurrentDirectory.ToList().ForEach(node =>
                    {
                        PreviousDirectory.Add((FileObjectInfo)node.Clone());
                    });
                    CurrentDirectory.Clear();
                    CurrentDirectory = new ObservableCollection<FileObjectInfo>();
                    try
                    {
                        var directories = new DirectoryInfo(((FileObjectInfo)obj).FullName).GetDirectories();
                        directories.ToList().ForEach(directory =>
                        {
                            CurrentDirectory.Add(new FileObjectInfo(directory));
                        });
                        var files = new DirectoryInfo(((FileObjectInfo)obj).FullName).GetFiles();
                        files.ToList().ForEach(file =>
                        {
                            CurrentDirectory.Add(new FileObjectInfo(file));
                        });
                    }
                    catch
                    {
                        MessageBox.Show("Drive not ready");
                    }
                }
                else if (((FileObjectInfo)obj).Type == FileObjectType.File)
                {
                    var data = Client.UploadFile($"{((FileObjectInfo)obj).FullName}");
                }
            }
        }
        #endregion

        #region REMOTE STORAGE ViewModel

        private FtpClient _client;
        private FtpFileObjectInfo _ftpSelectedFileObject;
        private ObservableCollection<FtpFileObjectInfo> _ftpCurrentDirectory;
        private string _ftpCurrentDirectoryFullName;

        public string FtpCurrentDirectoryFullName
        {
            get { return _ftpCurrentDirectoryFullName; }
            set { _ftpCurrentDirectoryFullName = value; RaisePropertyChanged("FtpCurrentDirectoryFullName"); }
        }

        public FtpFileObjectInfo FtpSelectedFileObject
        {
            get { return _ftpSelectedFileObject; }
            set { _ftpSelectedFileObject = value; RaisePropertyChanged("FtpSelectedFileObject"); }
        }

        public FtpClient Client
        {
            get { return _client; }
            private set { _client = value; RaisePropertyChanged("Client"); }
        }

        public ObservableCollection<FtpFileObjectInfo> FtpCurrentDirectory
        {
            get { return _ftpCurrentDirectory; }
            set { _ftpCurrentDirectory = value; RaisePropertyChanged("FtpCurrentDirectory"); }
        }

        private Core.RelayCommand _connectCommand;

        public Core.RelayCommand ConnectCommand
        {
            get
            {
                return _connectCommand ??
                    (_connectCommand = new Core.RelayCommand(obj =>
                    {
                        ConnectCommandMethod();
                    }));
            }
        }

        void ConnectCommandMethod()
        {
            try
            {
                if (Client != null)
                {
                    if (Client.CommandTransfer.Connected)
                    {
                        Client.CommandTransfer.Close();
                    }
                    if (Client.DataTransfer != null)
                    {
                        if (Client.DataTransfer.Connected)
                        {
                            Client.DataTransfer.Close();
                        }
                    }
                }
                Client = new FtpClient(SessionInfo);
                if (Client.ConnectToServer())
                {
                    var result = Client.GetFileObjectInfo();
                    if (!string.IsNullOrEmpty(result))
                    {
                        FtpCurrentDirectory = new ObservableCollection<FtpFileObjectInfo>();
                        var rows = result.Split("\r\n");
                        rows.Where(e => !string.IsNullOrEmpty(e)).ToList().ForEach(row =>
                        {
                            var ftpFileObjectInfo = new FtpFileObjectInfo(row);
                            FtpCurrentDirectory.Add(ftpFileObjectInfo);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something wrong with Client: {ex}");
            }
        }

        private Core.RelayCommand _remoteDirectoryBackCommand;
        private Core.RelayCommand _remoteDirectoryCommand;

        public Core.RelayCommand RemoteDirectoryBackCommand
        {
            get
            {
                return _remoteDirectoryBackCommand ??
                    (_remoteDirectoryBackCommand = new Core.RelayCommand(obj =>
                    {
                        RemoteDirectoryBackCommandMethod();
                    }));
            }
        }

        public Core.RelayCommand RemoteDirectoryCommand
        {
            get
            {
                return _remoteDirectoryCommand ??
                    (_remoteDirectoryCommand = new Core.RelayCommand(obj =>
                    {
                        RemoteDirectoryCommandMethod(obj);
                    }));
            }
        }

        void RemoteDirectoryBackCommandMethod()
        {
            FtpCurrentDirectoryFullName = "/";
            if (Client != null)
            {
                var result = Client.ChangeCurrentDirectory(FtpCurrentDirectoryFullName);
                FtpCurrentDirectory.Clear();
                FtpCurrentDirectory = new ObservableCollection<FtpFileObjectInfo>();
                if (!string.IsNullOrEmpty(result))
                {
                    var rows = result.Split("\r\n");
                    rows.Where(e => !string.IsNullOrEmpty(e)).ToList().ForEach(row =>
                    {
                        var ftpFileObjectInfo = new FtpFileObjectInfo(row);
                        FtpCurrentDirectory.Add(ftpFileObjectInfo);
                    });
                }
            }
        }

        void RemoteDirectoryCommandMethod(object obj)
        {
            if (obj is FtpFileObjectInfo)
            {
                if (((FtpFileObjectInfo)obj).Type == FileObjectType.Directory)
                {
                    FtpCurrentDirectoryFullName += (FtpCurrentDirectoryFullName == "/") ?
                        ((FtpFileObjectInfo)obj).Name :
                        ((FtpFileObjectInfo)obj).FullName;
                    var result = Client.ChangeCurrentDirectory(FtpCurrentDirectoryFullName);
                    FtpCurrentDirectory.Clear();
                    FtpCurrentDirectory = new ObservableCollection<FtpFileObjectInfo>();
                    if (!string.IsNullOrEmpty(result))
                    {
                        var rows = result.Split("\r\n");
                        rows.Where(e => !string.IsNullOrEmpty(e)).ToList().ForEach(row =>
                        {
                            var ftpFileObjectInfo = new FtpFileObjectInfo(row);
                            FtpCurrentDirectory.Add(ftpFileObjectInfo);
                        });
                    }
                }
                else if (((FtpFileObjectInfo)obj).Type == FileObjectType.File)
                {
                    var name = ((FtpFileObjectInfo)obj).Name;
                    var data = Client.DownloadFile($"{((FtpCurrentDirectoryFullName == "/") ? "" : FtpCurrentDirectoryFullName)}{((FtpFileObjectInfo)obj).FullName}");
                    if (data != null)
                    {
                        var path = "";
                        if (CurrentDirectory.Any(e => e.Type == FileObjectType.Drive))
                        {
                            path = ConfigurationManager.AppSettings["defaultSavePath"] + name;
                        }
                        else
                        {
                            path = SelectedFileObject.FullName + "\\" + name;
                        }
                        if (!string.IsNullOrEmpty(path))
                        {   
                            File.WriteAllBytes(path, data);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
