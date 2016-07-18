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
    class HelpCommands : ICommands
    {
        private Api _bot;
        private Regex pattern;

        public HelpCommands(Api bot)
        {
            _bot = bot;
            pattern = new Regex(@"^/help$");
        }

        public void Parse(ref Dictionary<long, ChatsSettings> chatsSettings, Update up)
        {
            var match = pattern.Match(up.Message.Text);
            var channel = up.Message.Chat.Id;

            if (match.Length > 0)
            {
                _bot.SendTextMessage(channel, "Комманды:\n" +
                    "/start - запуск сервиса оповещений\n" +
                    "/set mainchannel - переключение для нескольких каналов(чатов)\n" +
                    "/available (type/status) - просмотр статусов или типов тасков, которые доступны для добавления в рассылку\n" +
                    "/look (type/status) - просмотр статусов или типов тасков, которые попадают в рассылку\n" +
                    "/add (status/type) %имя% - добавление типов или статусов в оповещения\n" +
                    "/delete (status/type) %имя% - удаление типов или статусов из оповещений\n" +
                    "/status FN-4324 - статус задачи");
            }
        }
    }
}
