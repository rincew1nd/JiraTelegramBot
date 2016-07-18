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
    class ProjectCommands : ICommands
    {
        private Api _bot;
        private Regex pattern;

        public ProjectCommands(Api bot)
        {
            _bot = bot;
            pattern = new Regex("\"[^\"]*\"");
        }

        public void Parse(ref Dictionary<long, ChatsSettings> chatsSettings, Update up)
        {
            var match = pattern.Match(up.Message.Text).ToString();
            if (match.Length > 2) match = match.Replace("\"", "").ToLower();

            var channel = up.Message.Chat.Id;

            if (up.Message.Text.Contains("/look project"))
            {
                var text = "Проекты включенные в оповещения:\n";
                foreach (var project in chatsSettings[channel].GetProjects())
                    text += project + ", ";
                _bot.SendTextMessage(channel, text.Substring(0, text.Length - 2) + ".");
            }
            else if (up.Message.Text.Contains("/add project"))
            {
                if (match != "")
                    if (!chatsSettings[channel].GetProjects().Contains(match))
                    {
                        chatsSettings[channel].GetProjects().Add(match);
                        _bot.SendTextMessage(channel, "Добавлен проект " + match + " в список на оповещения.");
                    }
                    else
                        _bot.SendTextMessage(channel, "Проект " + match + " уже есть в списке на оповещения.");
                else
                    _bot.SendTextMessage(channel, "Неправильные аргументы. /add project \"TEST\"");
            }
            else if (up.Message.Text.Contains("/delete project"))
            {
                if (match != "")
                    if (chatsSettings[channel].GetProjects().Contains(match))
                    {
                        chatsSettings[channel].GetProjects().Remove(match);
                        _bot.SendTextMessage(channel, "Удален проект " + match + " из списка на оповещения.");
                    }
                    else
                        _bot.SendTextMessage(channel, "Проекта " + match + " нет в списке на оповещения.");
                else
                    _bot.SendTextMessage(channel, "Неправильные аргументы. /delete project \"TEST\"");
            }
        }
    }
}
