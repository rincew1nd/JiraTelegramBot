using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira_Telegram_notification.Settings
{
    class ExternalSettings
    {
        public string API_key;
        public List<UserSettings> user_settings;
    }

    class UserSettings
    {
        public long chatId;
        public string URL;
        public string login;
        public string password;
        public List<string> admins;
        public List<string> projects;
        public List<string> types;
        public List<string> statuses;
    }
}
