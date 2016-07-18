using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Jira_Telegram_notification.Commands
{
    interface ICommands
    {
        void Parse(ref Dictionary<long, ChatsSettings> chatsSettings, Update up);
    }
}
