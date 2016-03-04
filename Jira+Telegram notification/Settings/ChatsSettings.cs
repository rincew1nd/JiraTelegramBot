using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jira_Telegram_notification.Settings;
using TechTalk.JiraRestClient;

namespace Jira_Telegram_notification
{
    class ChatsSettings
    {
        private JiraSettings _jiraSettings;
        private JiraClient _jiraClient;
        private long _channelId = 0;

        private bool _isOperating = false;
        private bool _isLoading = false;
        private DateTime? _lastUpdateTime;

        private List<string> _admins;
        private Dictionary<int, string> _availableStatuses;
        private Dictionary<int, string> _availableTypes;
        private List<string> _projects;
        private List<string> _types;
        private List<string> _statuses;
        private Dictionary<string, string> _allTaskList;

        #region Конструктор
        public ChatsSettings(JiraSettings settings, long channelId, string admin)
        {
            #region Инициальзация переменных \ Variables initialization
            var credential = settings.GetCredentials();

            try
            {
                _jiraClient = new JiraClient(credential["server"], credential["login"], credential["password"]);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }

            _admins =  new List<string>();
            _availableStatuses = new Dictionary<int, string>();
            _availableTypes = new Dictionary<int, string>();
            _projects = new List<string>();
            _types = new List<string>();
            _statuses = new List<string>();
            _allTaskList = new Dictionary<string, string>();

            _isOperating = false;
            _isLoading = true;
            _lastUpdateTime = DateTime.Today;
            #endregion
            
            _jiraSettings = settings;
            _channelId = channelId;
            _admins.Add(admin);
        }
        #endregion

        #region Закончить или начать работу \ Stop or Start operating
        public ChatsSettings StartOperating()
        {
            _isOperating = true;
            return this;
        }

        public ChatsSettings StopOperating()
        {
            _isOperating = false;
            return this;
        }

        public ChatsSettings StartLoading()
        {
            _isLoading = true;
            return this;
        }

        public ChatsSettings StopLoading()
        {
            _isLoading = false;
            return this;
        }
        #endregion

        #region Установить админов \ Admin setup
        public string LoadAdmins()
        {
            //_admins = Utils.LoadAdmins(_channelId);
            return null;
        }

        public string AddAdmins(List<string> admins)
        {
            if (admins == null)
                return "Передан пустой список!";

            _admins.AddRange(admins);
            return null;
        }

        public string DeleteAdmins(List<string> admins)
        {
            if (admins == null)
                return "Передан пустой список!";

            foreach (var admin in admins)
                if (_admins.Contains(admin))
                    _admins.Remove(admin);
            return null;
        }
        #endregion

        #region За чем следить \ Types or statuses of task watching for
        public ChatsSettings LoadWatchingProjects(UserSettings settings)
        {
            settings.projects.ForEach(z => _projects.Add(z.ToLower()));
            return this;
        }

        public ChatsSettings LoadWatchingTypes(UserSettings settings)
        {
            settings.types.ForEach(z => _types.Add(z.ToLower()));
            return this;
        }

        public ChatsSettings LoadWatchingStatus(UserSettings settings)
        {
            settings.statuses.ForEach(z => _statuses.Add(z.ToLower()));
            return this;
        }

        public string AddWatchingProjects(List<string> projects)
        {
            if (projects == null)
                return "Передан пустой список!";

            _projects.AddRange(projects);
            return null;
        }

        public string AddWatchingTypes(List<string> types)
        {
            if (types == null)
                return "Передан пустой список!";

            _types.AddRange(types);
            return null;
        }

        public string AddWatchingStatus(List<string> statuses)
        {
            if (statuses == null)
                return "Передан пустой список!";

            _statuses.AddRange(statuses);
            return null;
        }

        public string DeleteWatchingProject(List<string> projects)
        {
            if (projects == null)
                return "Передан пустой список!";

            foreach (var project in projects)
                if (_projects.Contains(project))
                    _projects.Remove(project);
            return null;
        }

        public string DeleteWatchingTypes(List<string> types)
        {
            if (types == null)
                return "Передан пустой список!";

            foreach (var type in types)
                if (_types.Contains(type))
                    _types.Remove(type);
            return null;
        }

        public string DeleteWatchingStatus(List<string> statuses)
        {
            if (statuses == null)
                return "Передан пустой список!";

            foreach (var statuse in statuses)
                if (_statuses.Contains(statuse))
                    _statuses.Remove(statuse);
            return null;
        }
        #endregion

        #region Возможные типы и статусы задач \ Possible types or statuses of tasks
        public ChatsSettings LoadAvailableStatus()
        {
            //_types = Utils.LoadAvailableStatus(_channelId);
            return this;
        }

        public ChatsSettings LoadAvailableTypes()
        {
            //_types = Utils.LoadAvailableTypes(_channelId);
            return this;
        }

        public string AddAvailableStatus(Dictionary<int, string> availableStatuses)
        {
            if (availableStatuses == null)
                return "Передан пустой словарь!";
            else
                return Utils.ConcatDictionary(_availableStatuses, availableStatuses);
        }

        public string AddAvailableTypes(Dictionary<int, string> availableTypes)
        {
            if (availableTypes == null)
                return "Передан пустой словарь!";
            else
                return Utils.ConcatDictionary(_availableTypes, availableTypes);
        }
        #endregion

        #region Добавить статус задачи
        public void AddTask(string name, string status)
        {
            _allTaskList.Add(name, status);
        }

        public void AddTasks(Dictionary<string, string> tasks)
        {
            _allTaskList = tasks;
        }
        #endregion

        #region Геты Сеты \ Gets Sets
        public JiraClient GetJira()
        {
            return _jiraClient;
        }

        public long GetChannelId()
        {
            return _channelId;
        }

        public bool GetOperating()
        {
            return _isOperating;
        }

        public bool GetLoaded()
        {
            return _isLoading;
        }

        public DateTime? GetLastUpdateTime()
        {
            return _lastUpdateTime;
        }

        public void SetLastUpdateTime(DateTime? lastUpdateTime)
        {
            _lastUpdateTime = lastUpdateTime;
        }

        public List<string> GetAdmins()
        {
            return _admins;
        }

        public Dictionary<string, string> GetAllTasks()
        {
            return _allTaskList;
        }

        public Dictionary<int, string> GetAvailableStatuses()
        {
            return _availableStatuses;
        }

        public Dictionary<int, string> GetAvailableTypes()
        {
            return _availableTypes;
        }

        public List<string> GetStatuses()
        {
            return _statuses;
        }

        public List<string> GetTypes()
        {
            return _types;
        }

        public List<string> GetProjects()
        {
            return _projects;
        }
        #endregion
    }

    class JiraSettings
    {
        private readonly string _server = "";
        private readonly string _login = "";
        private readonly string _password = "";

        public JiraSettings(string server, string login, string password)
        {
            _server = server;
            _login = login;
            _password = password;
        }

        public Dictionary<string, string> GetCredentials()
        {
            return new Dictionary<string, string>
            {
                { "server", _server },
                { "login", _login },
                { "password", _password }
            };
        }
    }
}
