using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Jira_Telegram_notification.Commands
{
    class TypeCommands
    {
        private Telegram.Bot.Api _bot;
        private Regex pattern;

        public TypeCommands(Telegram.Bot.Api bot)
        {
            _bot = bot;
            pattern = new Regex("\"[^\"]*\"");
        }

        public void TypeCommandsParse(ref Dictionary<long, ChatsSettings> chatsSettings, Update up)
        {
            var match = pattern.Match(up.Message.Text).ToString();
            if (match.Length > 2) match = match.Replace("\"", "");

            var channel = up.Message.Chat.Id;

            if (up.Message.Text.Contains("/look type"))
            {
                var text = "Типы задачи включенные в оповещения:\n";
                foreach (var type in chatsSettings[channel].GetTypes())
                    text += type + ", ";
                _bot.SendTextMessage(channel, text.Substring(0, text.Length - 2) + ".");
            }
            else if (up.Message.Text.Contains("/add type"))
            {
                if (match != "")
                    if (chatsSettings[channel].GetAvailableTypes().ContainsValue(match))
                    {
                        if (!chatsSettings[channel].GetTypes().Contains(match))
                        {
                            chatsSettings[channel].GetTypes().Add(match);
                            _bot.SendTextMessage(channel, "Добавлен тип " + match + " в список на оповещения.");
                        }
                        else
                            _bot.SendTextMessage(channel, "Тип " + match + " уже есть в списке на оповещения.");
                    }
                    else
                        _bot.SendTextMessage(channel, "Типа " + match + " нет в списоке доступных для оповещения.");
                else
                    _bot.SendTextMessage(channel, "Неправильные аргументы. /add type \"bug\"");
            }
            else if (up.Message.Text.Contains("/delete type"))
            {
                if (match != "")
                    if (chatsSettings[channel].GetAvailableTypes().ContainsValue(match))
                        if (chatsSettings[channel].GetTypes().Contains(match))
                        {
                            chatsSettings[channel].GetTypes().Remove(match);
                            _bot.SendTextMessage(channel, "Удален тип " + match + " из списка на оповещения.");
                        }
                        else
                            _bot.SendTextMessage(channel, "Типа " + match + " нет в списке на оповещения.");
                    else
                        _bot.SendTextMessage(channel, "Типа " + match + " нет в списоке доступных для оповещения.\n/available type");
                else
                    _bot.SendTextMessage(channel, "Неправильные аргументы. /delete type \"bug\"");
            }
            else if (up.Message.Text.Contains("/available type"))
            {
                var str = "";
                foreach (var availableType in chatsSettings[channel].GetAvailableTypes())
                    str += availableType.Key + " - " + availableType.Value + "\n";

                _bot.SendTextMessage(channel, "Доступные типы задачи:\n" + str);
            }
        }
    }
}
