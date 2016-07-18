using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Jira_Telegram_notification.Commands
{
    class FeaturesCommands : ICommands
    {
        private Api _bot;
        private Regex pattern;

        public FeaturesCommands(Api bot)
        {
            _bot = bot;
            pattern = new Regex("\"[^\"]*\"");
        }

        public void Parse(ref Dictionary<long, ChatsSettings> chatsSettings, Update up)
        {
            var match = pattern.Match(up.Message.Text).ToString();
            if (match.Length > 2) match = match.Replace("\"", "").ToLower();

            var channel = up.Message.Chat.Id;

            if (up.Message.Text.Contains("/status"))
            {
                if (chatsSettings[channel].GetAllTasks().ContainsKey(match))
                    _bot.SendTextMessage(channel, "Статус задачи " + match + " -> " + chatsSettings[channel].GetAllTasks()[match]);
            }
        }
    }
}
