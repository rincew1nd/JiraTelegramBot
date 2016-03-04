using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jira_Telegram_notification.Commands;
using Jira_Telegram_notification.Settings;
using Newtonsoft.Json;
using Telegram;
using Telegram.Bot.Types;
using TechTalk.JiraRestClient;

namespace Jira_Telegram_notification
{
    class Program
    {
        private static List<JiraClient> _jiraClients;
        private static Dictionary<long, ChatsSettings> _chatsSettings;
        private static Telegram.Bot.Api bot;
        private static ExternalSettings externalSettings;
        
        static void Main(string[] args)
        {
            _jiraClients = new List<JiraClient>();
            _chatsSettings = new Dictionary<long, ChatsSettings>();


            externalSettings = JsonConvert.DeserializeObject<ExternalSettings>(Utils.ReadFile("\\Settings.json"));

            Task.Run(async () => await RunSearching());
            RunCommands().Wait();
        }

        static async Task RunCommands()
        {
            bot = new Telegram.Bot.Api(externalSettings.API_key);
            var me = await bot.GetMe();
            System.Console.WriteLine("Greetings from " + me.Username);

            var statusCommands = new StatusCommands(bot);
            var typeCommands = new TypeCommands(bot);
            var featuresCommands = new FeaturesCommands(bot);
            var projectCommands = new ProjectCommands(bot);
            var settingCommands = new SettingCommands(bot);


            var offset = 0;
            while (true)
            {
                var updates = new Update[0];
                try
                {
                    updates = await bot.GetUpdates(offset);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR WHILE GETTIGN UPDATES - " + ex.Message);
                }

                foreach (var up in updates)
                {
                    offset = up.Id + 1;

                    if (up.Message.Text != null)
                    {
                        var splitedMessage = up.Message.Text.Split(' ');
                        var channel = up.Message.Chat.Id;

                        //if (availableCommands.Contains(splitedMessage[0]))
                        //{
                        //    if (_chatsSettings.ContainsKey(channel) &&
                        //        _chatsSettings[channel].GetOperating() &&
                        //        !up.Message.Text.Contains("/help"))
                        //        if (!_chatsSettings[channel].GetLoaded())
                        //        {
                        //            statusCommands.StatusCommandsParse(_chatsSettings, up);
                        //            typeCommands.TypeCommandsParse(_chatsSettings, up);
                        //            featuresCommands.StatusCommandsParse(_chatsSettings, up);
                        //        }
                        //        else
                        //        {
                        //            if (_chatsSettings[channel].GetAllTasks().Count == 0)
                        //                await
                        //                    bot.SendTextMessage(channel,
                        //                        "Подождите пока задачи загрузятся. Пока ни одна задача не загрузилась");
                        //            else
                        //                await
                        //                    bot.SendTextMessage(channel, "Подождите пока задачи загрузятся. Последняя добавленная задача - " +
                        //                        _chatsSettings[channel].GetAllTasks().Last().Key + " от " + _chatsSettings[channel].GetLastUpdateTime());
                        //        }
                        //
                        //    if (up.Message.Type == MessageType.TextMessage)
                        //    {
                        //        if (up.Message.Text.Contains("/help"))
                        //        {
                        //            await bot.SendTextMessage(channel, "Комманды:\n" +
                        //                                           "/start - запуск сервиса оповещений\n" +
                        //                                           "/set mainchannel - переключение для нескольких каналов(чатов)\n" +
                        //                                           "/available (type/status) - просмотр статусов или типов тасков, которые доступны для добавления в рассылку\n" +
                        //                                           "/look (type/status) - просмотр статусов или типов тасков, которые попадают в рассылку\n" +
                        //                                           "/add (status/type) %имя% - добавление типов или статусов в оповещения\n" +
                        //                                           "/delete (status/type) %имя% - удаление типов или статусов из оповещений\n" +
                        //                                           "/status FN-4324 - статус задачи");
                        //        }
                        //    }
                        //}


                        if (up.Message.Type == MessageType.TextMessage)
                        {
                            settingCommands.StatusCommandsParse(externalSettings, ref _chatsSettings, up);
                            if (_chatsSettings.Count != 0)
                            {
                                statusCommands.StatusCommandsParse(ref _chatsSettings, up);
                                typeCommands.TypeCommandsParse(ref _chatsSettings, up);
                                featuresCommands.StatusCommandsParse(ref _chatsSettings, up);
                                projectCommands.Parse(ref _chatsSettings, up);
                            }
                        }
                    }
                }

                await Task.Delay(1000);
            }
        }

        private static async Task RunSearching()
        {
            try
            {
                foreach (var chatSettings in _chatsSettings.Values)
                {
                    if (chatSettings.GetOperating() && !chatSettings.GetLoaded())
                    {
                        foreach (var project in chatSettings.GetProjects())
                        {
                            foreach (var type in chatSettings.GetTypes())
                            {
                                var issues =
                                    chatSettings.GetJira()
                                        .EnumerateIssuesByQuery(
                                            "project = " + project + " AND issuetype = " + type + " AND updated >= -1m",
                                            null, 0);
                                foreach (var issue in issues)
                                {
                                    if (chatSettings.GetStatuses().Contains(issue.fields.status.name.ToLower()))
                                    {
                                        if (!chatSettings.GetAllTasks().ContainsKey(issue.key))
                                        {
                                            chatSettings.GetAllTasks().Add(issue.key, issue.fields.status.name.ToLower());

                                            await bot.SendTextMessage(chatSettings.GetChannelId(),
                                                chatSettings.GetJira().GetServerInfo().baseUrl + "/browse/" + issue.key +
                                                "\n" +
                                                "(" + issue.fields.issuetype.name + " | " + issue.fields.priority.name +
                                                ") " + issue.fields.summary + "\n" +
                                                "Находится в статусе " + issue.fields.status.name + "\n" +
                                                "Лейблы: " +
                                                ((issue.fields.labels.Count != 0)
                                                    ? issue.fields.labels.Aggregate((curr, next) => curr + ", " + next)
                                                    : "Нет"));

                                            Console.WriteLine(chatSettings.GetJira().GetServerInfo().baseUrl + "| " +
                                                              "New watched! " + issue.key + " : " + issue.fields.status.name);
                                        }
                                        else if (
                                            !chatSettings.GetAllTasks()[issue.key].Equals(
                                                issue.fields.status.name.ToLower()))
                                        {
                                            await bot.SendTextMessage(chatSettings.GetChannelId(),
                                                chatSettings.GetJira().GetServerInfo().baseUrl + "/browse/" + issue.key +
                                                "\n" +
                                                "(" + issue.fields.issuetype.name + " | " + issue.fields.priority.name +
                                                ") " + issue.fields.summary + "\n" +
                                                "Находится в статусе " + issue.fields.status.name + "\n" +
                                                "Лейблы: " +
                                                ((issue.fields.labels.Count != 0)
                                                    ? issue.fields.labels.Aggregate((curr, next) => curr + ", " + next)
                                                    : "Нет"));

                                            chatSettings.GetAllTasks()[issue.key] = issue.fields.status.name.ToLower();

                                            Console.WriteLine(chatSettings.GetJira().GetServerInfo().baseUrl + "| " +
                                                              issue.key + " : " + chatSettings.GetAllTasks()[issue.key] +
                                                              " -> " + issue.fields.status.name);
                                        }
                                    }
                                    else
                                    {
                                        if (chatSettings.GetAllTasks().ContainsKey(issue.key))
                                        {
                                            if (
                                                !chatSettings.GetAllTasks()[issue.key].Equals(
                                                    issue.fields.status.name.ToLower()))
                                            {
                                                Console.WriteLine(chatSettings.GetJira().GetServerInfo().baseUrl + "| " +
                                                                  issue.key + " : " + chatSettings.GetAllTasks()[issue.key] +
                                                                  " -> " + issue.fields.status.name);
                                                chatSettings.GetAllTasks()[issue.key] = issue.fields.status.name.ToLower();
                                            }
                                        }
                                        else
                                        {
                                            chatSettings.GetAllTasks().Add(issue.key, issue.fields.status.name.ToLower());
                                            Console.WriteLine(chatSettings.GetJira().GetServerInfo().baseUrl + "| " +
                                                              "New not watched! " + issue.key + " : " +
                                                              issue.fields.status.name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                await Task.Delay(10000);
                Task.Run(async () => await RunSearching());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                Task.Delay(10000);
                Task.Run(async () => await RunSearching());
            }
        }

        public static async Task LoadTasks(ChatsSettings chatSettings)
        {
            Console.WriteLine("Начинаю загрузку всех задач " + chatSettings.GetJira().GetServerInfo().baseUrl);
            chatSettings.StartLoading();

            foreach (var project in chatSettings.GetProjects())
                foreach (var issueType in chatSettings.GetTypes())
                {
                    foreach (var issue in chatSettings.GetJira().GetIssues(project, issueType))
                        if (!chatSettings.GetAllTasks().ContainsKey(issue.key))
                            chatSettings.AddTask(issue.key, issue.fields.status.name.ToLower());
                    Console.Write(chatSettings.GetAllTasks().Last().Key + " " + chatSettings.GetAllTasks().First().Key);
                }

            chatSettings.StopLoading();
            Console.WriteLine("Закончил загрузку всех задач " + chatSettings.GetJira().GetServerInfo().baseUrl + " " + chatSettings.GetChannelId());

            if (externalSettings.user_settings.Find(z => z.chatId == chatSettings.GetChannelId()) != null)
                chatSettings.StartOperating();
            else
                await bot.SendTextMessage(chatSettings.GetChannelId(), "Загрузка задач закончена!\n" +
                    "Теперь можно указать какие статусы нужно включить в оповещания (/add status \"string\"):\n" +
                    ((chatSettings.GetAvailableStatuses().Values.Count != 0) ? chatSettings.GetAvailableStatuses().Values.Aggregate((current, next) => current + ", " + next) : ""));
        }

        #region Не работает :(
        //static async Task FindUpdates(ChatsSettings chatSettings)
        //{
        //    Console.WriteLine("Voshel");
        //    var issues = (from i in chatSettings.GetJira().Issues
        //                  where i.Updated >= chatSettings.GetLastUpdateTime()
        //                  select i).Take(10);
        //    foreach (var issue in issues)
        //    {
        //        if (issue.Updated > chatSettings.GetLastUpdateTime() && chatSettings.GetTypes().Contains(issue.Type.Name.ToLower()))
        //        {
        //            if (chatSettings.GetStatuses().Contains(issue.fields.status.name.ToLower()))
        //            {
        //                if (!chatSettings.GetAllTasks().ContainsKey(issue.key))
        //                {
        //                    chatSettings.GetAllTasks().Add(issue.key, issue.fields.status.name);
        //    
        //                    await bot.SendTextMessage(chatSettings.GetChannelId(),
        //                        Utils.GetSettings("Server") + "browse/" + issue.Key + "\n" +
        //                        "(" + issue.Type.Name + " | " + issue.Priority.Name + ") " + issue.Summary + "\n" +
        //                        "Находится в статусе " + issue.fields.status.name);
        //                    Console.WriteLine("New watched! " + issue.key + " : " + issue.fields.status.name);
        //                }
        //                else if (chatSettings.GetAllTasks()[issue.key] != issue.fields.status.name)
        //                {
        //                    Console.WriteLine(issue.key + " : " + chatSettings.GetAllTasks()[issue.key] + " -> " + issue.fields.status.name);
        //    
        //                    if (issue.Updated != null && issue.Updated >= chatSettings.GetLastUpdateTime())
        //                        chatSettings.SetLastUpdateTime((DateTime)issue.Updated);
        //    
        //                    await bot.SendTextMessage(chatSettings.GetChannelId(),
        //                        Utils.GetSettings("Server") + "browse/" + issue.Key + "\n" +
        //                        "(" + issue.Type.Name + " | " + issue.Priority.Name + ") " + issue.Summary + "\n" +
        //                        "Находится в статусе " + issue.fields.status.name);
        //    
        //                    chatSettings.GetAllTasks()[issue.key] = issue.fields.status.name;
        //                }
        //            }
        //            else
        //            {
        //                if (chatSettings.GetAllTasks().ContainsKey(issue.key))
        //                {
        //                    if (!chatSettings.GetAllTasks()[issue.key].Equals(issue.fields.status.name))
        //                    {
        //                        Console.WriteLine(issue.key + " : " + chatSettings.GetAllTasks()[issue.key] +
        //                                          " -> " + issue.fields.status.name);
        //                        chatSettings.GetAllTasks()[issue.key] = issue.fields.status.name;
        //                    }
        //                }
        //                else
        //                {
        //                    Console.WriteLine("New not watched! " + issue.key + " : " + issue.fields.status.name);
        //                    chatSettings.GetAllTasks().Add(issue.key, issue.fields.status.name);
        //                }
        //            }
        //        }
        //    }
        //}
        #endregion
    }
}
