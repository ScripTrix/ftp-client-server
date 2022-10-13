using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client.Model
{
    public class FileObjectInfo : BindableBase, ICloneable
    {
        private string _name;
        private string _fullName;
        private string _rootFullName;
        private string _size;
        private FileObjectType _type;
        private string _lastAccess;
        private ImageSource _image;

        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged("Name"); }
        }

        public string FullName
        {
            get { return _fullName; }
            private set { _fullName = value; RaisePropertyChanged("FullName"); }
        }

        public string RootFullName
        {
            get { return _rootFullName; }
            set { _rootFullName = value; RaisePropertyChanged("RootFullName"); }
        }

        public string Size
        {
            get { return _size; }
            private set { _size = value; RaisePropertyChanged("Size"); }
        }

        public FileObjectType Type
        {
            get { return _type; }
            private set { _type = value; RaisePropertyChanged("Type"); }
        }

        public string LastAccess
        {
            get { return _lastAccess; }
            private set { _lastAccess = value; RaisePropertyChanged("LastAccess"); }
        }

        public ImageSource Image
        {
            get { return _image; }
            private set { _image = value; RaisePropertyChanged("Image"); }
        }

        public FileObjectInfo()
        {
        }

        public FileObjectInfo(DriveInfo drive)
            : this(drive.RootDirectory)
        {
        }

        public FileObjectInfo(FileSystemInfo info)
        {
            Name = info.Name;
            FullName = info.FullName;
            RootFullName = new DirectoryInfo(info.FullName).Root.FullName;
            LastAccess = info.LastAccessTime.ToString();
            if(info.FullName == info.Name)
            {
                Image = new BitmapImage(new Uri("pack://application:,,,/Icons/drive_16x16.ico"));
                Type = FileObjectType.Drive;
            }
            else if(info is DirectoryInfo)
            {
                Image = new BitmapImage(new Uri("pack://application:,,,/Icons/folder_16x16.ico"));
                Type = FileObjectType.Directory;
            }
            else if(info is FileInfo)
            {
                Image = new BitmapImage(new Uri("pack://application:,,,/Icons/file_16x16.ico"));
                Size = ((FileInfo)info).Length.ToString();
                Type = FileObjectType.File;
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
