using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace Server.BLL
{
    public class DirectoryEngine
    {
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

        public string GetPermission()
        {
            string result = null;
            return result;
        }

        public string GetContent()
        {
            string result = null;
            return result;
        }

        public string GetSize()
        {
            string result = null;
            return result;
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
            if(CurrentFtpPath != FtpRootPath)
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
