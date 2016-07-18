using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jira_Telegram_notification.Settings;
using TechTalk.JiraRestClient;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Jira_Telegram_notification.Commands
{
    class SettingCommands
    {
        private Api _bot;
        private Regex pattern;

        public SettingCommands(Api bot)
        {
            _bot = bot;
            pattern = new Regex("\"[^\"]*\"");
        }

        public void Parse(ExternalSettings externalSettings, ref Dictionary<long, ChatsSettings> chatsSettings, Update up)
        {
            var channel = up.Message.Chat.Id;
            var settings = externalSettings.user_settings.Find(z => z.chatId == channel);

            if (up.Message.Text.Contains("/start"))
            {
                var message = up.Message.Text.Split(' ');

                if (message.Length != 2 && settings != null)
                {
                    _bot.SendTextMessage(channel,
                            "Используйте /start (load|new) для загрузки или задания новых настроек канала");
                }
                else if (chatsSettings.ContainsKey(channel))
                    _bot.SendTextMessage(channel,
                            "Бот уже отслеживает жиру " + chatsSettings[channel].GetJira().GetServerInfo().baseUrl +
                            " на данном канале.");
                else if (message.Length == 4 && !chatsSettings.ContainsKey(channel))
                {
                    var chatSettings = new ChatsSettings(
                            new JiraSettings(message[1], message[2], message[3]),
                            up.Message.Chat.Id,
                            up.Message.From.Username
                        );
                    chatsSettings.Add(channel, chatSettings);

                    System.Console.WriteLine("Произошло соединение с " + message[1] + "\nНачинаю загрузку типов задач...");

                    _bot.SendTextMessage(channel,
                            "Укажите какие проекты вы хотите отслеживать (/add project \"string\"):\n");
                    _bot.SendTextMessage(channel,
                            "А так же какие типы задач (/add type \"string\"):\n" + LoadAllJiraTypes(chatSettings));
                    _bot.SendTextMessage(channel,
                            "После указания статусов можно запустить вотчер (/load tasks)");
                }
                else if (settings!= null && up.Message.Text == "/start load")
                {
                    var chatSettings = new ChatsSettings(
                            new JiraSettings(settings.URL, settings.login, settings.password),
                            up.Message.Chat.Id,
                            up.Message.From.Username
                        ).LoadWatchingProjects(settings)
                        .LoadWatchingTypes(settings)
                        .LoadWatchingStatus(settings);
                    Console.WriteLine("Запущено наблюдение за " + chatSettings.GetJira().GetServerInfo().baseUrl);
                    chatsSettings.Add(channel, chatSettings);

                    LoadAllJiraTypes(chatSettings);
                    LoadAllJiraStatuses(chatSettings);

                    Task.Run(async () => await Program.LoadTasks(chatSettings));
                }
                else if (message.Length != 4 || message.Length != 2)
                    _bot.SendTextMessage(channel,
                            "Предоставлены неверные аргументы./start URL username password или /start (load|new)");
            } else if (up.Message.Text == "/load tasks" && chatsSettings.ContainsKey(channel))
            {
                var chatSettings = chatsSettings[channel];
                Task.Run(async () => await Program.LoadTasks(chatSettings));

                System.Console.WriteLine("Начинаю загружать JIRA задачи из " + chatsSettings[channel].GetJira().GetServerInfo().baseUrl);
                _bot.SendTextMessage(channel,
                        "Начинаю загружать JIRA задачи из " + chatsSettings[channel].GetJira().GetServerInfo().baseUrl + ".\nЭто может занять некоторое время.");
            }
        }

        public string LoadAllJiraTypes(ChatsSettings chatSettings)
        {
            var typeDic = new Dictionary<int, string>();
            foreach (IssueType type in chatSettings.GetJira().GetIssueTypes())
                if (!typeDic.ContainsKey(type.id))
                   typeDic.Add(type.id, type.name.ToLower());
            chatSettings.AddAvailableTypes(typeDic);

            Console.WriteLine("Загружен список типов задач " + chatSettings.GetJira().GetServerInfo().baseUrl);
            return (typeDic.Values.Count!= 0) ? typeDic.Values.Aggregate((current, next) => current + ", " + next) : "";
        }

        public string LoadAllJiraStatuses(ChatsSettings chatSettings)
        {
            var statusDic = new Dictionary<int, string>();
            foreach (IssueStatus status in chatSettings.GetJira().GetIssueStatuses())
                if (!statusDic.ContainsKey(status.id))
                    statusDic.Add(status.id, status.name.ToLower());
            chatSettings.AddAvailableStatus(statusDic);

            Console.WriteLine("Загружен список сатусов задач " + chatSettings.GetJira().GetServerInfo().baseUrl);
            return (statusDic.Values.Count != 0) ? statusDic.Values.Aggregate((current, next) => current + ", " + next) : "";
        }
    }
}
