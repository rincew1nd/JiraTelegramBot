using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jira_Telegram_notification.Commands;
using Jira_Telegram_notification.Settings;
using Newtonsoft.Json;
using Telegram;
using Telegram.Bot.Types;
using TechTalk.JiraRestClient;
using Telegram.Bot;

namespace Jira_Telegram_notification
{
    class Program
    {
        private static List<JiraClient> _jiraClients;
        private static Dictionary<long, ChatsSettings> _chatsSettings;
        private static Api bot;
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
            var helpCommands = new HelpCommands(bot);

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

                    if (up.Message.Text != null && up.Message.Type == MessageType.TextMessage)
                    {
                        settingCommands.Parse(externalSettings, ref _chatsSettings, up);
                        if (_chatsSettings.Count != 0)
                        {
                            statusCommands.Parse(ref _chatsSettings, up);
                            typeCommands.Parse(ref _chatsSettings, up);
                            featuresCommands.Parse(ref _chatsSettings, up);
                            projectCommands.Parse(ref _chatsSettings, up);
                            helpCommands.Parse(ref _chatsSettings, up);
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
                                            "project = " + project + " AND issuetype = " + type +
                                            " AND updated >= -1m",
                                            null, 0);
                                foreach (var issue in issues)
                                {
                                    if (chatSettings.GetStatuses().Contains(issue.fields.status.name.ToLower()))
                                    {
                                        if (!chatSettings.GetAllTasks().ContainsKey(issue.key))
                                        {
                                            chatSettings.GetAllTasks()
                                                .Add(issue.key, issue.fields.status.name.ToLower());

                                            await bot.SendTextMessage(
                                                chatSettings.GetChannelId(),
                                                ConstrunctMessage(chatSettings, issue)
                                            );

                                            Console.WriteLine(
                                                String.Format(
                                                    chatSettings.GetJira().GetServerInfo().baseUrl,
                                                    "| ", "New watched! ", issue.key, " : ",
                                                    issue.fields.status.name
                                                )
                                            );
                                        }
                                        else if (
                                            !chatSettings.GetAllTasks()[issue.key].Equals(
                                                issue.fields.status.name.ToLower()))
                                        {
                                            await bot.SendTextMessage(
                                                chatSettings.GetChannelId(),
                                                ConstrunctMessage(chatSettings, issue)
                                            );

                                            chatSettings.GetAllTasks()[issue.key] =
                                                issue.fields.status.name.ToLower();
                                            
                                            Console.WriteLine(
                                                String.Format(
                                                    chatSettings.GetJira().GetServerInfo().baseUrl,
                                                    "| ", issue.key, " : ", chatSettings.GetAllTasks()[issue.key],
                                                    " -> ", issue.fields.status.name
                                                )
                                            );
                                        }
                                    }
                                    else
                                    {
                                        if (chatSettings.GetAllTasks().ContainsKey(issue.key))
                                        {
                                            if (!chatSettings.GetAllTasks()[issue.key].Equals(
                                                    issue.fields.status.name.ToLower()))
                                            {
                                                Console.WriteLine(
                                                    String.Format(
                                                        chatSettings.GetJira().GetServerInfo().baseUrl,
                                                        "| ", issue.key, " : ", chatSettings.GetAllTasks()[issue.key],
                                                        " -> ", issue.fields.status.name
                                                    )
                                                );
                                                chatSettings.GetAllTasks()[issue.key] =
                                                    issue.fields.status.name.ToLower();
                                            }
                                        }
                                        else
                                        {
                                            chatSettings.GetAllTasks()
                                                .Add(issue.key, issue.fields.status.name.ToLower());
                                            Console.WriteLine(
                                                String.Format(
                                                    chatSettings.GetJira().GetServerInfo().baseUrl,
                                                    "| ", "New not watched! ", issue.key, " : ",
                                                    issue.fields.status.name
                                                )
                                            );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            finally
            {
                await Task.Delay(10000);
                await Task.Run(async () => await RunSearching());
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
                    Console.Write(
                        String.Format(chatSettings.GetAllTasks().Last().Key,
                        " ", chatSettings.GetAllTasks().First().Key)
                    );
                }

            chatSettings.StopLoading();
            Console.WriteLine(
                $"Закончил загрузку всех задач {chatSettings.GetJira().GetServerInfo().baseUrl} " +
                $"{chatSettings.GetChannelId()}"
            );

            if (externalSettings.user_settings.Find(z => z.chatId == chatSettings.GetChannelId()) != null)
                chatSettings.StartOperating();
            else
                await bot.SendTextMessage(
                    chatSettings.GetChannelId(),
                    "Загрузка задач закончена!" + Environment.NewLine +
                    "Теперь можно указать какие статусы нужно включить в оповещания (/add status \"string\"):" + Environment.NewLine +
                    ((chatSettings.GetAvailableStatuses().Values.Count != 0) ?
                        chatSettings.GetAvailableStatuses().Values.Aggregate((current, next) => current + ", " + next) : 
                        ""));
        }

        private static string ConstrunctMessage(ChatsSettings chatSettings, Issue issue)
        {
            StringBuilder result = new StringBuilder();
            result.Append(chatSettings.GetJira().GetServerInfo().baseUrl);
            result.Append("/browse/");
            result.Append(issue.key);
            result.Append(Environment.NewLine);

            result.Append("(");
            result.Append(issue.fields.issuetype.name);
            result.Append(" | ");
            result.Append(issue.fields.priority.name);
            result.Append(") ");
            result.Append(issue.fields.summary);
            result.Append(Environment.NewLine);

            result.Append("Находится в статусе ");
            result.Append(issue.fields.status.name);
            result.Append(Environment.NewLine);

            result.Append("Лейблы: ");
            if (issue.fields.labels.Count != 0)
                result.Append(issue.fields.labels.Aggregate((curr, next) => curr + ", " + next));
            else
                result.Append("Нет");

            return result.ToString();
        }
    }
}
