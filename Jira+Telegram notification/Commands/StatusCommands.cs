using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Jira_Telegram_notification.Commands
{
    class StatusCommands
    {
        private Telegram.Bot.Api _bot;
        private Regex pattern;

        public StatusCommands(Telegram.Bot.Api bot)
        {
            _bot = bot;
            pattern = new Regex("\"[^\"]*\"");
        }

        public void StatusCommandsParse(ref Dictionary<long, ChatsSettings> chatsSettings, Update up)
        {
            var match = pattern.Match(up.Message.Text).ToString();
            if (match.Length > 2) match = match.Replace("\"", "").ToLower();

            var channel = up.Message.Chat.Id;

            if (up.Message.Text.Contains("/look status"))
            {
                var text = "Статусы задачи включенные в оповещения:\n";
                foreach (var status in chatsSettings[channel].GetStatuses())
                    text += status + ", ";
                _bot.SendTextMessage(channel, text.Substring(0, text.Length - 2) + ".");
            }
            else if (up.Message.Text.Contains("/add status"))
            {
                if (match != "")
                    if (chatsSettings[channel].GetAvailableStatuses().ContainsValue(match))
                    {
                        if (!chatsSettings[channel].GetStatuses().Contains(match))
                        {
                            chatsSettings[channel].GetStatuses().Add(match);
                            _bot.SendTextMessage(channel, "Добавлен статус " + match + " в список на оповещения.");
                        }
                        else
                            _bot.SendTextMessage(channel, "Статус " + match + " уже есть в списоке на оповещения.");

                    }
                    else
                        _bot.SendTextMessage(channel, "Статуса " + match + " нет в списоке доступных для оповещения.");
                else
                    _bot.SendTextMessage(channel, "Неправильные аргументы. /delete status \"developing\"");
            }
            else if (up.Message.Text.Contains("/delete status"))
            {
                if (match != "")
                    if (chatsSettings[channel].GetAvailableStatuses().ContainsValue(match))
                        if (chatsSettings[channel].GetStatuses().Contains(match))
                        {
                            chatsSettings[channel].GetStatuses().Remove(match);
                            _bot.SendTextMessage(channel, "Удален статус " + match + " из списка на оповещения.");
                        }
                        else
                            _bot.SendTextMessage(channel, "Cтатуса " + match + " нет в списке на оповещения.");
                    else
                        _bot.SendTextMessage(channel, "Статуса " + match + " нет в списоке доступных для оповещения.\n/available status");
                else
                    _bot.SendTextMessage(channel, "Неправильные аргументы. /delete status \"developing\"");
            }
            else if (up.Message.Text.Contains("/available status"))
            {
                var str = "";
                foreach (var availableStatus in chatsSettings[channel].GetAvailableStatuses())
                    str += availableStatus.Key + " - " + availableStatus.Value + "\n";

                _bot.SendTextMessage(channel, "Доступные статусы задачи:\n" + str);
            }
        }
    }
}
