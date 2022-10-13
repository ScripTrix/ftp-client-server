using Server.BLL;
using Server.FtpServer.DTO;
using Server.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Server.FtpServer
{
    public class CommandHandler
    {
        /// <summary>
        /// Метод обрабатывает команду USER и служит для предварительного связывания сессии и пользователя
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionUser(Session session, string[] props)
        {
            string result;
            session.IsAuthenticate = false;
            if (props.Length > 3)
            {
                result = $"501 Syntax error (bad parameter or argument): {string.Join(";", props.Skip(3).ToArray())} not understood.\r\n";
            }
            else if (props.Length > 1)
            {
                session.User = Session.Users.FirstOrDefault(e => e.Username == props[1]);
                if (session.User is null)
                {
                    result = "530 User cannot log in.\r\n" +
                                "\tError: The user name is incorrect.\r\n" +
                                "\tError details: An error occurred during the authentication process.\r\n" +
                             "530 End\r\n";
                }
                else
                {
                    session.PreviousCommand = session.CurrentCommand;
                    session.CurrentCommand = props[0];
                    if (true && session.User.Username == "anonymous")
                    {
                        result = "331 Password required (example: anonymous@mail.com).\r\n";
                    }
                    else
                    {
                        result = "331 Password required.\r\n";
                    }
                    if (props.Length == 3)
                    {
                        session.PreviousCommand = session.CurrentCommand;
                        session.CurrentCommand = props[0];
                        result = SessionPass(session, new string[] { "PASS", props[2] });
                    }
                }
            }
            else
            {
                result = "501 Syntax error (bad parameter or argument): The user name parameter required.\r\n";
            }
            return result;
        }

        /// <summary>
        /// Метод обрабатывает синтаксически неправильные команды
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionNone(Session session, string[] props)
            => "500 Syntax error, command cannot be interpreted\r\n";

        /// <summary>
        /// Метод обрабатывает команду PASS и выполняет инициализацию пользователя в сессии
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionPass(Session session, string[] props)
        {
            string result;
            if (session.CurrentCommand.ToUpper() != "USER")
            {
                result = "503 Failed Command Sequence: Enter the USER command first.\r\n";
            }
            else if (props.Length > 2)
            {
                result = $"501 Syntax error (bad parameter or argument): {string.Join(";", props.Skip(3).ToArray())} not understood.\r\n";
            }
            else if (props.Length == 2)
            {
                if (session.User == null)
                {
                    result = "530 Login failed! Authentication required (not logged in).\r\n";
                }
                else
                {
                    if (session.User.Username == "anonymous")
                    {
                        using (HashAlgorithm algorithm = SHA256.Create())
                            session.User.Hash = Encoding.UTF8.GetString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(props[1])));
                        result = "230 User authenticated, continue.\r\n";
                        session.IsAuthenticate = true;
                    }
                    else
                    {
                        string sha256;
                        using (HashAlgorithm algorithm = SHA256.Create())
                            sha256 = String.Concat(algorithm.ComputeHash(Encoding.UTF8.GetBytes(props[1])).Select(e => e.ToString("x2")));
                        if (session.User.Hash == sha256)
                        {
                            result = "230 User authenticated, continue.\r\n";
                            session.IsAuthenticate = true;
                        }
                        else
                        {
                            result = "530 Login failed! Authentication required (incorrect password).\r\n";
                        }
                    }
                    session.PreviousCommand = session.CurrentCommand;
                    session.CurrentCommand = props[0];
                }
            }
            else
            {
                result = "501 Syntax error (bad parameter or argument): The password parameter required.\r\n";
            }
            return result;
        }

        /// <summary>
        /// Метод обрабатывает команду FEAT и возвращает список функций, реализованных на FTP сервере
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionFeat(Session session, string[] props)
        {
            return "500";
        }

        /// <summary>
        /// Метод обрабатывает команду ABOR и прерывает передачу по каналу данных
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionAbor(Session session, string[] props)
        {
            return "500";
        }

        /// <summary>
        /// Метод обрабатывает команду Acct и предоставляет информацию об учетной записи
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionAcct(Session session, string[] props)
        {
            return "500";
        }

        /// <summary>
        /// Метод обрабатывает команду ADAT и предоставлят данные аутентификации/безопасности
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionAdat(Session session, string[] props)
        {
            return "500";
        }

        /// <summary>
        /// Метод обрабатывает команду ALLO и выделяет достаточное кол-во места на диске(Сервера) для получения файла
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionAllo(Session session, string[] props)
        {
            return "500";
        }

        /// <summary>
        /// Метод обрабатывает команду APPE и добавляет данные в файл (с созданием)
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionAppe(Session session, string[] props)
        {
            return "500";
        }

        /// <summary>
        /// Метод обрабатывает команду AUTH и реализует механизм аутентификации/безопасности
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionAuth(Session session, string[] props)
        {
            return "500";
        }

        /// <summary>
        /// Метод обрабатывает команду CCC и служит для изменения режима передачи в управляющем соединении с зашифрованного режима на режим открытого текста
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionCcc(Session session, string[] props)
        {
            return "500";
        }

        /// <summary>
        /// Метод обрабатывает команду CDUP и служит для смены текущего каталога на вышестоящий
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionCdup(Session session, string[] props)
        {
            return "500";
        }

        /// <summary>
        /// Метод обрабатывает команду CWD и служит для смены текущего каталога
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionCwd(Session session, string[] props)
        {
            string dir = null;
            if (props.Length > 2)
            {
                return $"501 Syntax error (bad parameter or argument): {string.Join(";", props.Skip(2).ToArray())} not understood.\r\n";
            }
            if (props.Length == 2)
            {
                dir = session.DirectoryEngine.ChangeWorkDirectory(props[1]);
            }
            else
            {
                return "500 Syntax error (bad parameter or argument).\r\n";
            }
            if (dir == null)
            {
                return "550 The system cannot find the file specified.\r\n" +
                            "\tError details: File system returned an error.\r\n" +
                       "550 End.\r\n";
            }
            if (dir == "")
            {
                return "530 Access denied.\r\n";
            }
            return $"257 \"{session.DirectoryEngine.GetWorkingDirectory()}\" is current directory.\r\n";
        }

        /// <summary>
        /// Метод обрабатывает команду DELE и служит для удаления файла (DELE filename) 
        /// </summary>
        /// <param name="session">Сессия FTP сервера, по отношению к которой выполняется команда</param>
        /// <param name="props">Массив, содержащий команду по индексу 0, вслед за которой идут параметры команды</param>
        /// <returns>Ответ FTP сервера ({код} {сообщение})</returns>
        public static string SessionDele(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionEnc(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionEprt(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionEpsv(Session session, string[] props)
        {
            TcpListener listener = null;
            bool isConnected = false;
            for (int i = 49152; i < 65535; i++)
            {
                try
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), i);
                    listener = new TcpListener(ipEndPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Port: {i} already in use.");
                }
                finally
                {
                    if (listener != null)
                    {
                        listener.Start();
                        Console.WriteLine($"Extended passive mode on (Port: {i})");
                        FtpServer.Reply(session, $"229 Entering Extended Passive Mode (|||{i}|).\r\n");
                        session.DataTransfer = listener.AcceptTcpClient();
                        listener.Stop();
                    }
                }
                if (session.DataTransfer.Connected)
                {
                    isConnected = true;
                    break;
                }
            }
            if (!isConnected)
            {
                return "451 Local error, Operation aborted.\r\n";
            }
            else
            {
                return "";
            }
        }

        public static string SessionHelp(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionHost(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionLang(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionList(Session session, string[] props)
        {
            if (props.Length == 1 || props.Length == 2)
            {
                FtpServer.Reply(session, "150 Opening ||ASCII|| mode data connection.\r\n");
                if (session.DataTransfer == null)
                {
                    return "425 Cannot open data connection.\r\n";
                }
                else
                {
                    if (props.Length == 2)
                    {
                        FtpServer.SendData(session, session.DirectoryEngine.GetContent(props[1]));
                    }
                    else
                    {
                        FtpServer.SendData(session, session.DirectoryEngine.GetContent(session.DirectoryEngine.CurrentFtpPath));
                    }
                    var flag = FtpServer.ReceiveCommand(session);
                    session.DataTransfer.Close();
                    return "226 Transfer complete.\r\n";
                }
            }
            else
            {
                return "501 Syntax error.\r\n";
            }
        }

        public static string SessionMdtm(Session session, string[] props)
        {
            if (props.Length > 1)
            {
                var length = session.DirectoryEngine.GetSize(props[1]);
                if (length == -1)
                {
                    return "500 File not exist.\r\n";
                }
                else
                {

                    return $"213 {length}\r\n";
                }
            }
            else
            {
                return "501 Syntax error\r\n";
            }
        }

        public static string SessionMic(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionMkd(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionMode(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionNlst(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionNoop(Session session, string[] props)
        {
            if (props.Length > 1)
                return "500 Command not understood.\r\n";
            return "200 noop command successful.\r\n";
        }

        public static string SessionOpts(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionPasv(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionPbsz(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionPort(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionProt(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionPwd(Session session, string[] props)
        {
            var dir = session.DirectoryEngine.GetWorkingDirectory();
            if (dir == null)
            {
                return "550 File not available, e.g. not found.\r\n";
            }
            return $"257 \"{dir}\" is current directory.\r\n";
        }

        public static string SessionRein(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionRest(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionRetr(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionRmd(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionRnfr(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionRnto(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionSite(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionSize(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionSmnt(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionStat(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionStor(Session session, string[] props)
        {
            if (props.Length < 2)
            {
                return "501 Syntax error.\r\n";
            }
            else
            {
                byte[] buffer;
                var path = "";
                var counter = 0;
                while (!path.Contains("."))
                {
                    counter++;
                    if (props.Length > counter)
                        path += $"{((counter == 1) ? props[counter] : $" {props[counter]}")}";
                    else
                        break;
                }
                if (!path.Contains("."))
                {
                    return "501 Syntax error.\r\n";
                }
                var length = session.DirectoryEngine.GetSize(path);
                if (length == -1)
                {
                    return "500 File not exist.\r\n";
                }
                else
                {
                    FtpServer.Reply(session, "150 Opening ||ASCII|| mode data connection.\r\n");
                    int result = session.DirectoryEngine.ReadFilePart(path, out buffer);
                    if (buffer != null)
                    {
                        FtpServer.SendData(session, Encoding.UTF8.GetString(buffer));
                        buffer = null;
                    }

                    var flag = FtpServer.ReceiveCommand(session);
                    session.DataTransfer.Close();
                    return "226 Transfer complete.\r\n";
                }
            }
        }

        public static string SessionStou(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionStru(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionSyst(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionType(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionXcup(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionXcwd(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionXmkd(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionXpwd(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionXrmd(Session session, string[] props)
        {
            return "500";
        }

        public static string SessionQuit(Session session, string[] props)
        {
            session.CommandTransfer.Client.Close();
            return "220";
        }
    }
}
