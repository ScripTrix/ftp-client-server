using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace Server.BLL
{
    public class DirectoryEngine
    {
        // TODO
        class Permission
        {
            public char Flag { get; set; } = '-';
            public char[] User { get; set; } = { '-', '-', '-' };
            public char[] Group { get; set; } = { '-', '-', '-' };
            public char[] Other { get; set; } = { '-', '-', '-' };
        }

        public static string FtpRootPath { get; private set; } = "/";

        public static string SystemRootPath { get; private set; } = ConfigurationManager.AppSettings["systemRootPath"];

        public string CurrentFtpPath { get; set; } = "/";


        public DirectoryEngine()
        {

        }

        public int ReadFilePart(string ftpPath, out byte[] buffer)
        {
            buffer = null;
            if (string.IsNullOrEmpty(ftpPath)) return -1;
            if (ftpPath[0] != '/') ftpPath = "/" + ftpPath;
            var start = ftpPath.LastIndexOf("/");
            var end = ftpPath.Length - start;
            var fileName = ftpPath.Substring(start, end);

            var systemPath = Regex.Replace(SystemRootPath + ftpPath.Replace("/", "\\"), @"(.+\/\.\.)", "");
            if (File.Exists(systemPath))
            {
                var fileLength = new FileInfo(systemPath).Length;
                buffer = new byte[fileLength];
                using (var fs = new FileStream(systemPath, FileMode.Open))
                {
                    return fs.Read(buffer, 0, (int)fileLength);
                }
            }
            systemPath = Regex.Replace(SystemRootPath + CurrentFtpPath.Replace("/", "\\") + ftpPath.Replace("/", "\\"), @"(.+\/\.\.)", "");
            if (File.Exists(systemPath))
            {
                var fileLength = new FileInfo(systemPath).Length;
                    buffer = new byte[fileLength];
                using (var fs = new FileStream(systemPath, FileMode.Open))
                {
                    return fs.Read(buffer, 0, (int)fileLength);
                }
            }
            return -1;
        }

        public string GetWorkingDirectory()
        {
            if (Directory.Exists(SystemRootPath + CurrentFtpPath.Replace("/", "\\")))
            {
                int start = CurrentFtpPath.LastIndexOf("/");
                int amount = CurrentFtpPath.Length - start;
                return CurrentFtpPath.Substring(start, amount);
            }
            return null;
        }

        // TODO
        public string GetPermission()
        {
            string result = null;
            return result;
        }

        public string GetContent(string ftpPath)
        {
            string result = "";
            if (string.IsNullOrEmpty(ftpPath)) ftpPath = "/";
            if (ftpPath[0] != '/') ftpPath = '/' + ftpPath;
            // абсолютный
            var dir = Regex.Replace(SystemRootPath + ftpPath.Replace("/", "\\"), @"(.+\/\.\.)", "");
            if (Directory.Exists(dir))
            {
                var directories = Directory.GetDirectories(dir);
                for (int i = 0; i < directories.Length; i++)
                {
                    var directoryInfo = new DirectoryInfo(directories[i]);
                    result += $"{directoryInfo.LastWriteTime}\t<DIR>\t{directoryInfo.Name}\r\n";
                }
                if (result != "") result += "\r\n";
                var files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    var fileInfo = new FileInfo(files[i]);
                    result += $"{fileInfo.LastWriteTime}\t{fileInfo.Length}\t{fileInfo.Name}\r\n";
                }
                return result;
            }
            // относительный
            dir = Regex.Replace(SystemRootPath + CurrentFtpPath.Replace("/", "\\") + ftpPath.Replace("/", "\\"), @"(.+\/\.\.)", "");
            if (Directory.Exists(dir))
            {
                var directories = Directory.GetDirectories(dir);
                for (int i = 0; i < directories.Length; i++)
                {
                    var directoryInfo = new DirectoryInfo(directories[i]);
                    result += $"{directoryInfo.LastWriteTime}\t<DIR>\t{directoryInfo.Name}\r\n";
                }
                if (result != "") result += "\r\n";
                var files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    var fileInfo = new FileInfo(files[i]);
                    result += $"{fileInfo.LastWriteTime}\t{fileInfo.Length}\t{fileInfo.Name}\r\n";
                }
                return result;
            }
            return null;
        }

        public long GetSize(string ftpPath)
        {
            if (string.IsNullOrEmpty(ftpPath)) return -1;
            if (ftpPath.IndexOf('/') != 0) ftpPath = '/' + ftpPath;
            var start = ftpPath.LastIndexOf("/");
            var end = ftpPath.Length - start;
            var fileName = ftpPath.Substring(start, end);

            var systemPath = Regex.Replace(SystemRootPath + ftpPath.Replace("/", "\\"), @"(.+\/\.\.)", "");
            if (File.Exists(systemPath))
            {
                return new FileInfo(systemPath).Length;
            }
            systemPath = Regex.Replace(SystemRootPath + CurrentFtpPath.Replace("/", "\\") + ftpPath.Replace("/", "\\"), @"(.+\/\.\.)", "");
            if (File.Exists(systemPath))
            {
                return new FileInfo(systemPath).Length;
            }
            return -1;
        }

        public string Delete()
        {
            string result = null;
            return result;
        }

        // Возможны 2 варианта: абсолютный путь(прописывается от корня ftp), относительный(в текущем каталоге ftp сессии)
        public string ChangeWorkDirectory(string ftpPath)
        {
            string result = null;
            if (string.IsNullOrEmpty(ftpPath)) ftpPath = "/";
            if (ftpPath[0] != '/') ftpPath = '/' + ftpPath;
            // абсолютный
            var dir = Regex.Replace(SystemRootPath + ftpPath.Replace("/", "\\"), @"(.+\/\.\.)", "");
            if (Directory.Exists(dir))
            {
                if (dir.Contains(SystemRootPath))
                {
                    CurrentFtpPath = dir.Replace(SystemRootPath, "").Replace("\\", "/");
                    return CurrentFtpPath;
                }
                return "";
            }
            // относительный
            dir = Regex.Replace(SystemRootPath + CurrentFtpPath.Replace("/", "\\") + ftpPath.Replace("/", "\\"), @"(.+\/\.\.)", "");
            if (Directory.Exists(dir))
            {
                if (dir.Contains(SystemRootPath))
                {
                    CurrentFtpPath = dir.Replace(SystemRootPath, "").Replace("\\", "/");
                    return CurrentFtpPath;
                }
                return "";
            }
            return result;
        }

        public string CurrentDirectoryUp()
        {
            string result = null;
            if (CurrentFtpPath != FtpRootPath)
            {
                var part = CurrentFtpPath.Substring(0, CurrentFtpPath.LastIndexOf("/"));
                if (part == "") part = "/";
                if (Directory.Exists(Path.Combine(SystemRootPath, part.Replace("/", "\\"))))
                {
                    CurrentFtpPath = part;
                    result = CurrentFtpPath;
                }
            }
            else
            {
                CurrentFtpPath = "/";
                result = CurrentFtpPath;
            }
            return result;
        }
    }
}
