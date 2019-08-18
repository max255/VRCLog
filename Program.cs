using Colorful;
using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Console = Colorful.Console;

namespace VRCLog
{
    class Program
    {
        private static string LogPath;
        private static string LogFile = null;

        private static Color LogColorDefault = Color.DarkGreen;
        private static Color LogColorPlayer = Color.Yellow;
        private static Color LogColorPlayerJoined = Color.FromArgb(0, 255, 0);
        private static Color LogColorPlayerLeft = Color.Red;
        private static Color LogColorLink = Color.Blue;
        private static Color LogColorAction = Color.Cyan;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;               // настраиваем событие для закрытия консоли

            Console.WriteLine(Resources.Logo, LogColorDefault);             // выводим лого
            
            try                                                             // читаем конфиг
            {
                var cfg = new Config();
                LogPath = cfg.GetParameter("path");
            }
            catch (Exception e)                                             // при ошибке - выводим текст ошибки и выходим
            {
                Console.WriteLine(e.Message, LogColorDefault);
                Console.ReadKey();
                Environment.Exit(0);
            }

            if (LogPath == "")                                              // проверяем настройки, выходим при ошибке
            {
                Console.WriteLine("Параметр path не найден в файле config.cfg", LogColorDefault);
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.WriteLine("Путь к папке логов: " + LogPath, LogColorDefault);

            using (FileSystemWatcher watcher = new FileSystemWatcher())     // запускаем watcher для отслеживания изменений в директории
            {
                watcher.Path = LogPath;
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
                watcher.Filter = "*.txt";
                watcher.Created += OnCreated;
                watcher.Changed += OnChanged;
                watcher.EnableRaisingEvents = true;

                Console.Write("Ожидание нового файла лога... ", LogColorDefault);

                while (LogFile is null)                                     // ждем пока появится новый файл лога
                {
                    var files = Directory.GetFiles(LogPath);

                    foreach (var file in files)                             // пока ждем юзаем костыль для правильной работы watcher
                    {
                        using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            stream.ReadByte();
                        }
                    }

                    Thread.Sleep(1000);
                }
            }

            Console.WriteLine(LogFile, LogColorDefault);
                                                                            // файл найден, открываем поток и читаем все что можно
            var fs = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (var sr = new StreamReader(fs))
            {
                string line;

                while (true)
                {
                    line = sr.ReadLine();
                    if (line != null)                                       // ищем кусочки текста, затем передаем строку на разборку
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
                        Thread.Sleep(1);                                    // обязательная задержка, иначе получим 100 % загрузку ядра
                    }
                }
            }
        }

        #region LogPriners

        private static void PrintPlayerJoin(string line)
        {
            var rxp = new Regex("(.+) Log .+ OnPlayerJoined (.+)");
            var result = rxp.Match(line);

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),
                new Formatter("Join", LogColorPlayerJoined),
                new Formatter(result.Groups[2], LogColorPlayer)
            };

            Console.WriteLineFormatted("{0} [Player] {1} {2}", LogColorDefault, parts);
        }

        private static void PrintPlayerLeft(string line)
        {
            var rxp = new Regex("(.+) Log .+ OnPlayerLeft (.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var player = result.Groups[2];

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),
                new Formatter("Left", LogColorPlayerLeft),
                new Formatter(result.Groups[2], LogColorPlayer)
            };

            Console.WriteLineFormatted("{0} [Player] {1} {2}", LogColorDefault, parts);
        }

        private static void PrintRoomJoining(string line)
        {
            var rxp = new Regex("(.+) Log .+ Joining (w.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var roomid = result.Groups[2];

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),
                new Formatter(result.Groups[2], LogColorLink)
            };

            Console.WriteLineFormatted("{0} [Room] Joining: {1}", LogColorDefault, parts);
        }

        private static void PrintRoomEntering(string line)
        {
            var rxp = new Regex("(.+) Log .+ Entering Room: (.+)");
            var result = rxp.Match(line);
            var date = result.Groups[1];
            var name = result.Groups[2];

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),
                new Formatter(result.Groups[2], LogColorLink)
            };

            Console.WriteLineFormatted("{0} [Room] Entering: {1}", LogColorDefault, parts);
        }

        private static void PrintVoteToKickInitiation(string line)
        {
            var rxp = new Regex(@"(.+) Log .+userToKickId=(.+),.+VRCPlayer\S+ (.+) .+Id=(.+) target.+");
            var result = rxp.Match(line);

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),          //date
                new Formatter("VoteToKick", Color.Red),
                new Formatter(result.Groups[3], LogColorPlayer),           //playerInitName
                new Formatter(result.Groups[4], LogColorLink),             //playerInitId
                new Formatter(result.Groups[2], LogColorLink)              //playerToKickId
            };

            Console.WriteLineFormatted("{0} [Moderation] {1} {2} [{3} -> {4}]", LogColorDefault, parts);
        }

        private static void PrintVideo(string line)
        {
            var rxp = new Regex(@"(.+) Log .+\[AVProVideo\] Opening (.+)\s\(.+");
            var result = rxp.Match(line);

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),        //date
                new Formatter(result.Groups[2], LogColorLink)            //link
            };

            Console.WriteLineFormatted("{0} [Video] Loading: {1}", LogColorDefault, parts);
        }

        private static void PrintVoteToKick(string line)
        {
            var rxp = new Regex("(.+) Log .+ReceiveVoteToKickInitiation on ModerationManager for (.+)");
            var result = rxp.Match(line);

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),          //date
                new Formatter("VoteToKick", Color.Red),
                new Formatter(result.Groups[2], LogColorPlayer)            //player
            };

            Console.WriteLineFormatted("{0} [Moderation] {1} {2}", LogColorDefault, parts);
        }

        private static void PrintMute(string line)
        {
            var rxp = new Regex("(.+) Log .+MuteChangeRPC on ModerationManager for (.+)");
            var result = rxp.Match(line);

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),          //date
                new Formatter("Mute", LogColorAction),
                new Formatter(result.Groups[2], LogColorPlayer)            //player
            };

            Console.WriteLineFormatted("{0} [Moderation] {1} {2}", LogColorDefault, parts);
        }

        private static void PrintBlock(string line)
        {
            var rxp = new Regex("(.+) Log .+BlockStateChangeRPC on ModerationManager for (.+)");
            var result = rxp.Match(line);

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),          //date
                new Formatter("Block", LogColorAction),
                new Formatter(result.Groups[2], LogColorPlayer)            //player
            };

            Console.WriteLineFormatted("{0} [Moderation] {1} {2}", LogColorDefault, parts);
        }

        private static void PrintPortal(string line)
        {
            var rxp = new Regex(@"(.+) Log .+ConfigurePortal on Portals.+VRCPlayer\[Remote\] (\S+)\s.+");
            var result = rxp.Match(line);

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),          //date
                new Formatter("Create portal", LogColorAction),
                new Formatter(result.Groups[2], LogColorPlayer)            //player
            };

            Console.WriteLineFormatted("{0} [Action] {1} {2}", LogColorDefault, parts);
        }

        private static void PrintEmote(string line)
        {
            var rxp = new Regex(@"(.+) Log .+PlayEmoteRPC on VRCPlayer\[Remote\]\s(\S+).+");
            var result = rxp.Match(line);

            var parts = new Formatter[]
            {
                new Formatter(result.Groups[1], LogColorDefault),          //date
                new Formatter("Emote", LogColorAction),
                new Formatter(result.Groups[2], LogColorPlayer)            //player
            };

            Console.WriteLineFormatted("{0} [Action] {1} {2}", LogColorDefault, parts);
        }

        #endregion

        #region Events

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

        #endregion
    }
}
