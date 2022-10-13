using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client.Model
{
    public class FtpFileObjectInfo : BindableBase
    {
        private string _name;
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
            get { return $"/{Name}"; }
        }

        public string Size
        {
            get { return _size; }
            set { _size = value; RaisePropertyChanged("Size"); }
        }

        public FileObjectType Type
        {
            get { return _type; }
            set { _type = value; RaisePropertyChanged("Type"); }
        }

        public string LastAccess
        {
            get { return _lastAccess; }
            set { _lastAccess = value; RaisePropertyChanged("LastAccess"); }
        }

        public ImageSource Image
        {
            get { return _image; }
            set { _image = value; RaisePropertyChanged("Image"); }
        }

        public FtpFileObjectInfo()
        {
        }

        public FtpFileObjectInfo(string data)
        {
            var nodes = data.Split("\t");
            LastAccess = nodes[0];
            Name = nodes[2];
            if(nodes[1] == "<DIR>")
            {
                Type = FileObjectType.Directory;
                Image = new BitmapImage(new Uri("pack://application:,,,/Icons/folder_16x16.ico"));
            }
            else
            {
                Type = FileObjectType.File;
                Image = new BitmapImage(new Uri("pack://application:,,,/Icons/file_16x16.ico"));
                Size = nodes[1];
            }
        }

        public static ObservableCollection<FtpFileObjectInfo> ParseFtpFileObjets(string data)
        {
            var rows = data.Split("\r\n");
            var collection = new ObservableCollection<FtpFileObjectInfo>();
            rows.Where(e => !string.IsNullOrEmpty(e)).ToList().ForEach(row =>
            {
                var ftpFileObjectInfo = new FtpFileObjectInfo(row);
                collection.Add(ftpFileObjectInfo);
            });
            return collection;
        }
    }
}
