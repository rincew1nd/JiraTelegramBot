using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Jira_Telegram_notification
{
    class Utils
    {
        public static string ReadFile(string path)
        {
            var result = "";

            using (var sr = new StreamReader(Directory.GetCurrentDirectory() + path))
            {
                result += sr.ReadToEnd();
            }
            return result;
        }

        public static string ConcatDictionary<K, V>(Dictionary<K, V> target, Dictionary<K, V> source)
        {
            foreach (var element in source)
                if (!target.ContainsKey(element.Key))
                    target.Add(element.Key, element.Value);
                else
                    return "В словаре уже присутствует ключ " + element;

            return null;
        }
    }
}
