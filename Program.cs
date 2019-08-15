using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace VRCLog
{
    class Program
    {
        private static string LogPath;
        private static string LogFile = null;

        private const ConsoleColor DefaultColor = ConsoleColor.DarkGreen;
        private const ConsoleColor PlayerColor = ConsoleColor.Yellow;
        private const ConsoleColor PlayerJoinedColor = ConsoleColor.Green;
        private const ConsoleColor PlayerLeftColor = ConsoleColor.Red;
        private const ConsoleColor LinkColor = ConsoleColor.Blue;
        private const ConsoleColor ActionColor = ConsoleColor.Cyan;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(Resources.Logo);

            

            Console.CancelKeyPress += Console_CancelKeyPress;

            try
            {
                var cfg = new Config();
                LogPath = cfg.GetParameter("path");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
                Environment.Exit(0);
            }

            if (LogPath == "")
            {
                Console.WriteLine("Параметр path не найден в файле config.cfg");
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.WriteLine("Путь к папке логов: " + LogPath);

            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = LogPath;
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
                watcher.Filter = "*.txt";
                watcher.Created += OnCreated;
                watcher.Changed += OnChanged;
                watcher.EnableRaisingEvents = true;

                Console.Write("Ожидание нового файла лога... ");

                while (LogFile is null)
                {
                    var files = Directory.GetFiles(LogPath);

                    foreach (var file in files)
                    {
                        using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            stream.ReadByte();
                        }
                    }

                    Thread.Sleep(1000);
                }
            }

            Console.WriteLine(LogFile);

            var fs = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (var sr = new StreamReader(fs))
            {
                string line;

                while (true)
                {
                    line = sr.ReadLine();
                    if (line != null)
                    {
                        if (line.Contains("[NetworkManager] OnPlayerJoined ")) PrintPlayerJoin(line);
                        if (line.Contains("[NetworkManager] OnPlayerLeft ")) PrintPlayerLeft(line);
                        if (line.Contains("[RoomManager] Joining w")) PrintRoomJoining(line);
                        if (line.Contains("[RoomManager] Entering Room:")) PrintRoomEntering(line);
                        if (line.Contains("[AVProVideo] Opening")) PrintVideo(line);
                        if (line.Contains("ReceiveVoteToKickInitiation userToKickId")) PrintVoteToKickInitiation(line);

                        if (line.Contains("[Network Processing] RPC invoked ReceiveVoteToKickInitiation")) PrintVoteToKick(line);
                        if (line.Contains("[Network Processing] RPC invoked MuteChangeRPC")) PrintMute(line);
                        if (line.Contains("[Network Processing] RPC invoked BlockStateChangeRPC")) PrintBlock(line);
                        if (line.Contains("[Network Processing] RPC invoked ConfigurePortal")) PrintPortal(line);
                        if (line.Contains("[Network Processing] RPC invoked PlayEmoteRPC")) PrintEmote(line);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
        }

        private static void PrintPlayerJoin(string line)
        {
            var rxp = new Regex("(.+) Log .+ OnPlayerJoined (.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var player = result.Groups[2];

            Console.ForegroundColor = DefaultColor;         Console.Write(date + " [Player] ");
            Console.ForegroundColor = PlayerJoinedColor;    Console.Write("Join ");
            Console.ForegroundColor = PlayerColor;          Console.WriteLine(player);

            Console.ForegroundColor = DefaultColor;
        }

        private static void PrintPlayerLeft(string line)
        {
            var rxp = new Regex("(.+) Log .+ OnPlayerLeft (.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var player = result.Groups[2];

            Console.ForegroundColor = DefaultColor; Console.Write(date + " [Player] ");
            Console.ForegroundColor = PlayerLeftColor; Console.Write("Left ");
            Console.ForegroundColor = PlayerColor; Console.WriteLine(player);

            Console.ForegroundColor = DefaultColor;
        }

        private static void PrintRoomJoining(string line)
        {
            var rxp = new Regex("(.+) Log .+ Joining (w.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var roomid = result.Groups[2];

            Console.ForegroundColor = DefaultColor; Console.Write(date + " [Room] Joining: ");
            Console.ForegroundColor = LinkColor; Console.WriteLine(roomid);

            Console.ForegroundColor = DefaultColor;
        }

        private static void PrintRoomEntering(string line)
        {
            var rxp = new Regex("(.+) Log .+ Entering Room: (.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var name = result.Groups[2];

            Console.ForegroundColor = DefaultColor; Console.WriteLine(date + " [Room] Entering: " + name);
        }

        private static void PrintVoteToKickInitiation(string line)
        {
            var rxp = new Regex(@"(.+) Log .+userToKickId=(.+),.+VRCPlayer\S+ (.+) .+Id=(.+) target.+");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var playerToKickId = result.Groups[2];
            var playerInitName = result.Groups[3];
            var playerInitId = result.Groups[4];

            Console.ForegroundColor = DefaultColor; Console.Write(date + " [Moderation] ");
            Console.ForegroundColor = ConsoleColor.Red; Console.Write("VoteToKick ");
            Console.ForegroundColor = PlayerColor; Console.Write(playerInitName + " ");
            Console.ForegroundColor = LinkColor; Console.Write(playerInitId);
            Console.ForegroundColor = DefaultColor; Console.Write(" -> ");
            Console.ForegroundColor = LinkColor; Console.WriteLine(playerToKickId);
            Console.ForegroundColor = DefaultColor;
        }

        private static void PrintVideo(string line)
        {
            var rxp = new Regex(@"(.+) Log .+\[AVProVideo\] Opening (.+)\s\(.+");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var link = result.Groups[2];

            Console.ForegroundColor = DefaultColor; Console.Write(date + " [Video] Loading: ");
            Console.ForegroundColor = LinkColor; Console.WriteLine(link);

            Console.ForegroundColor = DefaultColor;
        }

        private static void PrintVoteToKick(string line)
        {
            var rxp = new Regex("(.+) Log .+ReceiveVoteToKickInitiation on ModerationManager for (.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var player = result.Groups[2];

            Console.ForegroundColor = DefaultColor; Console.Write(date + " [Moderation] ");
            Console.ForegroundColor = ActionColor; Console.Write("VoteToKick ");
            Console.ForegroundColor = PlayerColor; Console.WriteLine(player);

            Console.ForegroundColor = DefaultColor;
        }

        private static void PrintMute(string line)
        {
            var rxp = new Regex("(.+) Log .+MuteChangeRPC on ModerationManager for (.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var player = result.Groups[2];

            Console.ForegroundColor = DefaultColor; Console.Write(date + " [Moderation] ");
            Console.ForegroundColor = ActionColor; Console.Write("Mute ");
            Console.ForegroundColor = PlayerColor; Console.WriteLine(player);

            Console.ForegroundColor = DefaultColor;
        }

        private static void PrintBlock(string line)
        {
            var rxp = new Regex("(.+) Log .+BlockStateChangeRPC on ModerationManager for (.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var player = result.Groups[2];

            Console.ForegroundColor = DefaultColor; Console.Write(date + " [Moderation] ");
            Console.ForegroundColor = ActionColor; Console.Write("Block ");
            Console.ForegroundColor = PlayerColor; Console.WriteLine(player);

            Console.ForegroundColor = DefaultColor;
        }

        private static void PrintPortal(string line)
        {
            var rxp = new Regex(@"(.+) Log .+ConfigurePortal on Portals.+VRCPlayer\[Remote\] (\S+)\s.+");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var player = result.Groups[2];

            Console.ForegroundColor = DefaultColor; Console.Write(date + " [Action] ");
            Console.ForegroundColor = ActionColor; Console.Write("Create portal ");
            Console.ForegroundColor = PlayerColor; Console.WriteLine(player);

            Console.ForegroundColor = DefaultColor;
        }

        private static void PrintEmote(string line)
        {
            var rxp = new Regex(@"(.+) Log .+PlayEmoteRPC on VRCPlayer\[Remote\]\s(\S+).+");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var player = result.Groups[2];

            Console.ForegroundColor = DefaultColor; Console.Write(date + " [Action] ");
            Console.ForegroundColor = ActionColor; Console.Write("Emote ");
            Console.ForegroundColor = PlayerColor; Console.WriteLine(player);

            Console.ForegroundColor = DefaultColor;
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            var watcher = (FileSystemWatcher)sender;
            watcher.EnableRaisingEvents = false;

            LogFile = e.FullPath;
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            var watcher = (FileSystemWatcher)sender;
            watcher.EnableRaisingEvents = false;

            LogFile = e.FullPath;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
