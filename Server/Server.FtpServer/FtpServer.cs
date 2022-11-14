using Server.DAL;
using Server.DAL.Interfaces;
using Server.FtpServer.DTO;
using Server.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server.FtpServer
{
    public class FtpServer : IFtpServer
    {
        public Dictionary<Session, ThreadWrapper> SessionsThreads { get; private set; }

        public Thread SessionsThreadsManagerThread { get; private set; }

        public Thread AcceptorThread { get; private set; }

        public bool Started { get; private set; }

        public DB DB { get; private set; }

        public Socket TcpListener { get; private set; }

        public IPAddress IpAddress { get; set; }

        public int Port { get; set; }

        CancellationTokenSource ManagerThreadCts { get; set; }

        CancellationTokenSource AcceptorThreadCts { get; set; }

        Dictionary<Session, CancellationTokenSource> SessionsThreadsCts { get; set; }

        public FtpServer()
        {

        }

        public FtpServer(string ipAddress, int port)
        {
            try
            {
                DB = new DB();
                Session.Users = DB.SelectRegisteredUsers().ToList();
                IpAddress = IPAddress.Parse(ipAddress);
                Port = port;
                Start();
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"IP address can't be null\n{ex}");
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"IP address format exception:\n{ex}");
            }
        }

        public void Start()
        {
            try
            {
                Started = true;
                TcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                TcpListener.Bind(new IPEndPoint(IpAddress, Port));
                TcpListener.Listen(100);
                AcceptorThread = new Thread(AcceptConnections);
                AcceptorThread.Start();
                SessionsThreads = new Dictionary<Session, ThreadWrapper>();
                SessionsThreadsManagerThread = new Thread(ManageSessionsThreads);
                SessionsThreadsManagerThread.Start();
                Console.WriteLine("Server successfully started");
            }
            catch (Exception ex)
            {
                Started = false;
                Console.WriteLine($"Something wrong with starting server:\n{ex}");
            }
        }

        public void Stop()
        {
            try
            {
                Started = false;
                while (true)
                {
                    if (AcceptorThread.IsAlive)
                    {
                        AcceptorThread.Abort();
                    }
                    if (AcceptorThread.ThreadState == ThreadState.Stopped)
                    {
                        break;
                    }
                }
                TcpListener.Close();

                while (true)
                {
                    if (SessionsThreadsManagerThread.IsAlive)
                    {
                        SessionsThreadsManagerThread.Abort();
                    }
                    if (SessionsThreadsManagerThread.ThreadState == ThreadState.Stopped)
                    {
                        break;
                    }
                }
                Console.WriteLine("FTP server was stoped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Something wrong with stopping server:\n{ex}");
            }
        }

        public void AcceptConnections()
        {
            while (Started)
            {
                var session = new Session();
                var maxValue = SessionsThreads.Keys.Count;
                if (maxValue == 0)
                {
                    session.Id = 0;
                }
                else
                {
                    session.Id = maxValue;
                }
                session.CommandTransfer = TcpListener.Accept();
                session.DirectoryEngine = new BLL.DirectoryEngine();
                if (session.CommandTransfer.Connected)
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    var sessionThread = new Thread(delegate ()
                    {
                        ProcessSession(session, cts.Token);
                    });
                    SessionsThreads[session] = new ThreadWrapper() { Thread = sessionThread, Cts = cts };
                    sessionThread.Start();
                }
                Thread.Sleep(1000);
            }
        }

        public static void Reply(Session session, string data)
        {
            if (session.CommandTransfer.Connected)
            {
                byte[] byteData = Encoding.UTF8.GetBytes(data);
                var bytesSent = Convert.ToInt64(session.CommandTransfer.Send(byteData));
                Console.WriteLine($"{bytesSent} bytes sent to client session: {session.Id}");
            }
        }

        //public static byte[] ReceiveData(Session session)
        //{
        //    NetworkStream stream = session.DataTransfer.GetStream();

        //    byte[] fileSizeBytes = new byte[4];
        //    int bytes = stream.Read(fileSizeBytes, 0, 4);
        //    int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);

        //    int bytesLeft = dataLength;
        //    byte[] data = new byte[dataLength];

        //    int bufferSize = 8192;
        //    int bytesRead = 0;

        //    while (bytesLeft > 0)
        //    {
        //        int curDataSize = Math.Min(bufferSize, bytesLeft);
        //        if (session.DataTransfer.Available < curDataSize)
        //            curDataSize = session.DataTransfer.Available;

        //        bytes = stream.Read(data, bytesRead, curDataSize);

        //        bytesRead += curDataSize;
        //        bytesLeft -= curDataSize;
        //    }
        //    return data;
        //}

        public static byte[] ReceiveData(Session session)
        {
            if (session.DataTransfer.Connected && session.CommandTransfer.Available == 0)
            {
                byte[] curr = null;

                NetworkStream stream = new NetworkStream(session.DataTransfer);
                while (session.DataTransfer.Connected)
                {
                    byte[] fileSizeBytes = new byte[4]; // для получения размера следующего сообщения
                    int bytes = stream.Read(fileSizeBytes, 0, 4); // получаем размер из сокета
                    int dataLength = BitConverter.ToInt32(fileSizeBytes, 0); // конвертируем в инт

                    int bytesLeft = dataLength;

                    curr = new byte[dataLength]; // выделяем массив для получения данных из сокета

                    int bufferSize = 8192;
                    int bytesRead = 0;

                    while (bytesLeft > 0)
                    {
                        int curDataSize = Math.Min(bufferSize, bytesLeft);
                        if (session.DataTransfer.Available < curDataSize && session.DataTransfer.Available != 0)
                            curDataSize = session.DataTransfer.Available;

                        bytes = stream.Read(curr, bytesRead, curDataSize);

                        bytesRead += curDataSize;
                        bytesLeft -= curDataSize;
                    }
                    return curr;
                }
            }
            return null;
        }

        public static void SendData(Session session, byte[] data)
        {
            if (session.DataTransfer.Connected)
            {
                using (var ns = new NetworkStream(session.DataTransfer))
                {
                    int bufferSize = 8192;
                    byte[] dataSize = BitConverter.GetBytes(data.Length);

                    ns.Write(dataSize, 0, 4);

                    int bytesSent = 0;
                    int bytesLeft = data.Length;

                    while (bytesLeft > 0)
                    {
                        int curDataSize = Math.Min(bufferSize, bytesLeft);

                        ns.Write(data, bytesSent, curDataSize);

                        bytesSent += curDataSize;
                        bytesLeft -= curDataSize;
                    }
                    Console.WriteLine($"{bytesSent} bytes of data sent to client session: {session.Id}");
                }
            }
        }

        public static string ReceiveCommand(Session session)
        {
            var currByte = new Byte[1];
            var buffer = new StringBuilder();
            while (session.CommandTransfer.Connected && currByte[0] != ((byte)('\n')))
            {
                currByte = new Byte[1];
                var byteCounter = session.CommandTransfer.Receive(currByte, currByte.Length, SocketFlags.None);
                if (byteCounter.Equals(1))
                {
                    buffer.Append(Encoding.UTF8.GetString(currByte));
                }
            }
            return buffer.ToString().Trim(new char[] { '\r', '\n' });
        }

        public static void InitSessionCommands(Session session)
        {
            session.AvailableCommands = new Dictionary<Commands, Delegate>();
            session.AvailableCommands[Commands.NONE] = new Func<Session, string[], string>(CommandHandler.SessionNone);
            session.AvailableCommands[Commands.ADAT] = new Func<Session, string[], string>(CommandHandler.SessionAdat);
            session.AvailableCommands[Commands.CCC] = new Func<Session, string[], string>(CommandHandler.SessionCcc);
            session.AvailableCommands[Commands.ENC] = new Func<Session, string[], string>(CommandHandler.SessionEnc);
            session.AvailableCommands[Commands.FEAT] = new Func<Session, string[], string>(CommandHandler.SessionFeat);
            session.AvailableCommands[Commands.HELP] = new Func<Session, string[], string>(CommandHandler.SessionHelp);
            session.AvailableCommands[Commands.HOST] = new Func<Session, string[], string>(CommandHandler.SessionHost);
            session.AvailableCommands[Commands.LANG] = new Func<Session, string[], string>(CommandHandler.SessionLang);
            session.AvailableCommands[Commands.MIC] = new Func<Session, string[], string>(CommandHandler.SessionMic);
            session.AvailableCommands[Commands.MODE] = new Func<Session, string[], string>(CommandHandler.SessionMode);
            session.AvailableCommands[Commands.NOOP] = new Func<Session, string[], string>(CommandHandler.SessionNoop);
            session.AvailableCommands[Commands.OPTS] = new Func<Session, string[], string>(CommandHandler.SessionOpts);
            session.AvailableCommands[Commands.PASS] = new Func<Session, string[], string>(CommandHandler.SessionPass);
            session.AvailableCommands[Commands.PBSZ] = new Func<Session, string[], string>(CommandHandler.SessionPbsz);
            session.AvailableCommands[Commands.PROT] = new Func<Session, string[], string>(CommandHandler.SessionProt);
            session.AvailableCommands[Commands.QUIT] = new Func<Session, string[], string>(CommandHandler.SessionQuit);
            session.AvailableCommands[Commands.REIN] = new Func<Session, string[], string>(CommandHandler.SessionRein);
            session.AvailableCommands[Commands.STRU] = new Func<Session, string[], string>(CommandHandler.SessionStru);
            session.AvailableCommands[Commands.SYST] = new Func<Session, string[], string>(CommandHandler.SessionSyst);
            session.AvailableCommands[Commands.TYPE] = new Func<Session, string[], string>(CommandHandler.SessionType);
            session.AvailableCommands[Commands.USER] = new Func<Session, string[], string>(CommandHandler.SessionUser);
        }

        public static void ExtendSessionCommands(Session session)
        {
            session.AvailableCommands[Commands.ABOR] = new Func<Session, string[], string>(CommandHandler.SessionAbor);
            session.AvailableCommands[Commands.ACCT] = new Func<Session, string[], string>(CommandHandler.SessionAcct);
            session.AvailableCommands[Commands.ALLO] = new Func<Session, string[], string>(CommandHandler.SessionAllo);
            session.AvailableCommands[Commands.APPE] = new Func<Session, string[], string>(CommandHandler.SessionAppe);
            session.AvailableCommands[Commands.AUTH] = new Func<Session, string[], string>(CommandHandler.SessionAuth);
            session.AvailableCommands[Commands.CDUP] = new Func<Session, string[], string>(CommandHandler.SessionCdup);
            session.AvailableCommands[Commands.CWD] = new Func<Session, string[], string>(CommandHandler.SessionCwd);
            session.AvailableCommands[Commands.DELE] = new Func<Session, string[], string>(CommandHandler.SessionDele);
            session.AvailableCommands[Commands.EPRT] = new Func<Session, string[], string>(CommandHandler.SessionEprt);
            session.AvailableCommands[Commands.EPSV] = new Func<Session, string[], string>(CommandHandler.SessionEpsv);
            session.AvailableCommands[Commands.LIST] = new Func<Session, string[], string>(CommandHandler.SessionList);
            session.AvailableCommands[Commands.MDTM] = new Func<Session, string[], string>(CommandHandler.SessionMdtm);
            session.AvailableCommands[Commands.MKD] = new Func<Session, string[], string>(CommandHandler.SessionMkd);
            session.AvailableCommands[Commands.NLST] = new Func<Session, string[], string>(CommandHandler.SessionNlst);
            session.AvailableCommands[Commands.PASV] = new Func<Session, string[], string>(CommandHandler.SessionPasv);
            session.AvailableCommands[Commands.PORT] = new Func<Session, string[], string>(CommandHandler.SessionPort);
            session.AvailableCommands[Commands.PWD] = new Func<Session, string[], string>(CommandHandler.SessionPwd);
            session.AvailableCommands[Commands.REST] = new Func<Session, string[], string>(CommandHandler.SessionRest);
            session.AvailableCommands[Commands.RETR] = new Func<Session, string[], string>(CommandHandler.SessionRetr);
            session.AvailableCommands[Commands.RMD] = new Func<Session, string[], string>(CommandHandler.SessionRmd);
            session.AvailableCommands[Commands.RNFR] = new Func<Session, string[], string>(CommandHandler.SessionRnfr);
            session.AvailableCommands[Commands.RNTO] = new Func<Session, string[], string>(CommandHandler.SessionRnto);
            session.AvailableCommands[Commands.SITE] = new Func<Session, string[], string>(CommandHandler.SessionSite);
            session.AvailableCommands[Commands.SIZE] = new Func<Session, string[], string>(CommandHandler.SessionSize);
            session.AvailableCommands[Commands.SMNT] = new Func<Session, string[], string>(CommandHandler.SessionSmnt);
            session.AvailableCommands[Commands.STAT] = new Func<Session, string[], string>(CommandHandler.SessionStat);
            session.AvailableCommands[Commands.STOR] = new Func<Session, string[], string>(CommandHandler.SessionStor);
            session.AvailableCommands[Commands.STOU] = new Func<Session, string[], string>(CommandHandler.SessionStou);
            session.AvailableCommands[Commands.XCUP] = new Func<Session, string[], string>(CommandHandler.SessionXcup);
            session.AvailableCommands[Commands.XCWD] = new Func<Session, string[], string>(CommandHandler.SessionXcwd);
            session.AvailableCommands[Commands.XMKD] = new Func<Session, string[], string>(CommandHandler.SessionXmkd);
            session.AvailableCommands[Commands.XPWD] = new Func<Session, string[], string>(CommandHandler.SessionXpwd);
            session.AvailableCommands[Commands.XRMD] = new Func<Session, string[], string>(CommandHandler.SessionXrmd);
        }

        static void ProcessSession(Session session, CancellationToken token)
        {
            try
            {
                if (session != null)
                {
                    //InitSessionCommands(session);
                    Reply(session, "220 Connected to FtpServer.\r\n");
                    while (session.CommandTransfer.Connected || !token.IsCancellationRequested)
                    {
                        if (session.IsAuthenticate)
                        {
                            ExtendSessionCommands(session);
                        }
                        else
                        {
                            InitSessionCommands(session);
                        }
                        var commandInfo = ReceiveCommand(session).Split(' ');
                        Commands command = EnumParser.Parse<Commands>(commandInfo[0]);
                        if (session.AvailableCommands.ContainsKey(command))
                        {
                            Reply(session, (string)session.AvailableCommands[command].DynamicInvoke(session, commandInfo));
                        }
                        else
                        {
                            Reply(session, "530 Please login with USER and PASS.\r\n");
                        }
                    }

                    session.CommandTransfer.Close();
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Process session arguments is incorrect:\n{ex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process session exception:\n{ex}");
            }
        }

        public void ManageSessionsThreads()
        {
            while (true)
            {
                foreach (var session in SessionsThreads)
                {

                    if (session.Value.Thread.ThreadState == ThreadState.Unstarted)
                    {
                        continue;
                    }
                    // NEED FIX
                    else if (!session.Key.CommandTransfer.Connected)
                    {
                        Console.WriteLine($"Session with ID: {session.Key.Id} was completed");
                        session.Value.Cts.Cancel();
                        session.Value.Thread.Join();
                        session.Value.Cts.Dispose();
                    }
                }

                SessionsThreads = SessionsThreads
                    .Where(e => e.Value.Thread.IsAlive)
                    .ToDictionary(e => e.Key, e => e.Value);
                if (SessionsThreads.Count == 0 && !Started)
                {
                    break;
                }
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }
    }
}
