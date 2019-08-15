using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VRCLog
{
    public class Config
    {
        private string data;

        public Config()
        {
            var configpath = Directory.GetCurrentDirectory();
            configpath = Path.Combine(configpath, "config.cfg");

            if (File.Exists(configpath))
            {
                data = File.ReadAllText(configpath);
            }
            else
            {
                File.AppendAllText(configpath, "");
                throw new Exception("Файл config.cfg не найден! Создан пустой файл.");
            }
        }

        public string GetParameter(string param, string def = "")
        {
            Regex rxp = new Regex("^" + param + " ?= ?(.+)$", RegexOptions.Multiline);
            var result = rxp.Match(data);

            if (result.Success && result.Groups.Count > 0)
            {
                return result.Groups[1].Value.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
            }

            return def;
        }
    }
}
