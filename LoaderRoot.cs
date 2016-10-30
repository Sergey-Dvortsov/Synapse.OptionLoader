namespace Synapse.OptionLoader
{
    using System;
    using System.Windows;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    using Ecng.Common;
    using Ecng.Xaml;
    using Ecng.Serialization;
    using MoreLinq;

    using StockSharp.Algo;
    using StockSharp.BusinessEntities;
    using StockSharp.Logging;
    using StockSharp.Messages;
    using StockSharp.Quik;
    using StockSharp.SmartCom;
    using StockSharp.Algo.Candles;
    using StockSharp.Algo.Candles.Compression;
    using StockSharp.SmartCom.Native;
    using StockSharp.Algo.Storages;
    using Synapse.Common;
    using StockSharp.Localization;

    public class LoaderRoot : BaseViewModel
    {

        private bool _shootDown;
        private string _settingsPath;
        private bool _isConfigLoad = false;
        private string _appFolder;
        private object _lock = new object();
        private long _currentLoaderId;

        private static LoaderRoot _root;

        /// <summary>
        /// Частный конструктор
        /// </summary>
        private LoaderRoot()
        {
            //Compose();
            //Init();
        }

        #region Events

        /// <summary>
        /// Генерируется при создании коннектора
        /// </summary>
        public event Action<Connector, eConnectorType> ConnectorCreated = delegate { };
        private void OnConnectorCreated(Connector connector, eConnectorType type)
        {
            if (ConnectorCreated != null)
                ConnectorCreated.Invoke(connector, type);
        }

        /// <summary>
        /// Генерируется при изменении состояния торговой сессии
        /// </summary>
        public event Action<eSessionState> SessionStateChanged = delegate { };
        public void OnSessionStateChanged(eSessionState state)
        {
            if (SessionStateChanged != null)
                SessionStateChanged.Invoke(state);
        }

        /// <summary>
        /// Генерируется при изменении состояния соединения
        /// </summary>
        public event Action<eConnectionState> ConnectionStateChanged = delegate { };
        private void OnConnectionStateChanged(eConnectionState state)
        {
            ServerConnectionState = state;
            if (ConnectionStateChanged != null)
                ConnectionStateChanged.Invoke(state);
        }

        /// <summary>
        /// Генерируется для отображения нового сообщения в статусной строке
        /// </summary>
        public event Action<string> NewStatusMessage = delegate { };
        public void OnNewStatusMessage(string message)
        {
            if (NewStatusMessage != null)
                NewStatusMessage.Invoke(message);
        }

        #endregion

        #region Properties

        public Configuration Config { set; get; }

        private Connector _connector;
        /// <summary>
        ///Коннектор торговой платформы 
        /// </summary>
        public Connector Connector
        {
            get { return _connector; }
            set
            {
                _connector = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Менеджер логгирорования
        /// </summary>
        public LogManager LogManager { set; get; }

        private eConnectorType _connectorType;
        /// <summary>
        /// Тип коннектора
        /// </summary>
        public eConnectorType ConnectorType
        {
            get { return _connectorType; }
            set
            {
                if (_connectorType == value)
                    return;
                _connectorType = value;
                NotifyPropertyChanged();
            }
        }

        private eConnectionState _serverConnectionState;
        /// <summary>
        /// Хранилище
        /// </summary>
        public eConnectionState ServerConnectionState
        {
            get { return _serverConnectionState; }
            set
            {
                if (_serverConnectionState == value)
                    return;
                _serverConnectionState = value;
                NotifyPropertyChanged();
            }
        }

        public IFolderBuilder FolderBuilder { get; set; }

        private TimeSpan _marketTimeDelay;
        /// <summary>
        /// Минимальная задержка активности сервера, после которой будет считаться, что соединение нарушено
        /// </summary>
        public TimeSpan MarketTimeDelay
        {
            get { return _marketTimeDelay; }
            set
            {
                if (_marketTimeDelay == value)
                    return;
                _marketTimeDelay = value;
            }
        }

        private TimeSpan _marketTimeError;
        /// <summary>
        /// Ошибка времени, которая используется при определении состояния торговой сессии
        /// </summary>
        public TimeSpan MarketTimeError
        {
            get { return _marketTimeError; }
            set
            {
                if (_marketTimeError == value)
                    return;
                _marketTimeError = value;
            }
        }

        private eSessionState _sessionState;
        /// <summary>
        /// Состояние торговой сессии
        /// </summary>
        public eSessionState SessionState
        {
            get { return _sessionState; }
            set
            {
                if (_sessionState == value)
                    return;
                _sessionState = value;
                NotifyPropertyChanged();
            }
        }

        public SettingsStorage GeneralSettings { private set; get; }

        public List<OptionChain> Chains { set; get; }

        #endregion

        #region Public methods

        /// <summary>
        /// Возвращает экземпляр(singleton) класса
        /// </summary>
        public static LoaderRoot GetInstance()
        {
            if (_root == null)
            {
                lock (typeof(LoaderRoot))
                {
                    _root = new LoaderRoot();
                }
            }
            return _root;
        }

        /// <summary>
        /// Вызывает соединение
        /// </summary>
        public void Connect()
        {
            OnConnect();
        }

        /// <summary>
        /// Разрывает соединение
        /// </summary>
        public void Disconnect()
        {
            Connector.Disconnect();
        }

        public void UpdateConfig(Configuration config)
        {
            if (Connector == null ||
                Config.ConnectorType != config.ConnectorType ||
                (Config.ConnectorType == eConnectorType.Quik && (Config.LuaAddress != config.LuaAddress || Config.LuaLogin != config.LuaLogin)) ||
                (Config.ConnectorType == eConnectorType.SmartCom && (!Config.SmartAddress.IsEqual(config.SmartAddress) ||
                !Config.SmartLive.IsEqual(config.SmartLive) || !Config.SmartDemo.IsEqual(config.SmartDemo))))
            {
                CreateConnector(config);
            }

            MarketTimeDelay = config.MarketTimeDelay;
            MarketTimeError = config.MarketTimeError;

            this.Config = config;

        }

        public void ShootDown()
        {
            Connector.RegisteredSecurities.ForEach(security => Connector.UnRegisterSecurity(security));

            Connector.RegisteredTrades.ForEach(security => Connector.UnRegisterTrades(security));

            Connector.RegisteredMarketDepths.ForEach(security => Connector.UnRegisterMarketDepth(security));

            Connector.RegisteredPortfolios.ForEach(portfolio => Connector.UnRegisterPortfolio(portfolio));

            _shootDown = true;

            Connector.Disconnect();
        }

        public void Save()
        {
            if (GeneralSettings == null)
                GeneralSettings = new SettingsStorage();
            SaveGeneral();
        }

        private void SaveGeneral()
        {
            GeneralSettings.SetValue("Configuration", Config.Save());
            new XmlSerializer<SettingsStorage>().Serialize(GeneralSettings, _settingsPath);
        }

        public void Load()
        {
            if (File.Exists(_settingsPath))
            {
                GeneralSettings = new XmlSerializer<SettingsStorage>().Deserialize(_settingsPath);
                Load(GeneralSettings);
            }
        }

        public override void Load(SettingsStorage storage)
        {
            try
            {
                this.AddWarningLog(string.Format("{0}", "Загрузка основных настроек"));

                if (_isConfigLoad)
                    return;

                if (!storage.Contains("Configuration"))
                    throw new ArgumentNullException(nameof(storage));

                if (Config == null)
                    Config = new Configuration();

                Config.Load(storage);

                UpdateConfig(Config);

                if (Connector == null)
                    throw new ArgumentNullException("Не удалоcь создать коннектор!");

                SessionState = DateTimeOffset.Now.SessionState(Config.IsDemo);

                if (Config.ConnectOnRun)
                    Connect();

                _isConfigLoad = true;

            }
            catch (Exception ex)
            {
                this.AddErrorLog(string.Format("{0} / {1}", ex.Message, ex.StackTrace));
                OnNewStatusMessage(ex.Message);
            }

        }

        #endregion

        #region Private Functions

        // выполняет начальные настройки 
        public void Init()
        {
            try
            {
                _appFolder = FolderBuilder.GetAppFolder();
                var logFolder = FolderBuilder.GetLogFolder();
                _settingsPath = Path.Combine(_appFolder, "generalSettings.xml");

                LogManager = new LogManager();
                LogManager.Listeners.Add(new FileLogListener(Path.Combine(logFolder, string.Format("{0}.{1}", FolderBuilder.GetAppName(), "log"))));
                LogManager.Sources.Add(this);

                if (Config == null)
                    Config = new Configuration();

                ServerConnectionState = eConnectionState.StopConnect | eConnectionState.Disconnected;

                Chains = new List<OptionChain>();

            }
            catch (Exception ex)
            {
                this.AddErrorLog(string.Format("{0} / {1}", ex.Message, ex.StackTrace));
            }

        }

        private void CreateConnector(Configuration config)
        {

            try
            {

                if (Connector != null && Connector.ConnectionState == ConnectionStates.Connected)
                    throw new Exception("Нельзя создать новый коннектор при активном соединении.");

                if (Connector != null)
                {
                    if (LogManager.Sources.Contains(Connector))
                        LogManager.Sources.Remove(Connector);
                    Connector.Dispose();
                }

                switch (config.ConnectorType)
                {
                    case eConnectorType.SmartCom:

                        if (config.SmartAddress == null)
                            throw new Exception("Не задан адрес сервера SmartCom.");

                        if (config.SmartAddress.IsEqual(SmartComAddresses.Demo) && (string.IsNullOrWhiteSpace(config.SmartDemo.Login) || string.IsNullOrWhiteSpace(config.SmartDemo.Password)))
                        {
                            this.AddWarningLog(string.Format("{0}", "Не заданы логин и/или пароль демо-счета SmartCom"));
                            OnNewStatusMessage("Не заданы логин и/или пароль для демо-счета SmartCom!");
                            return;
                        }

                        if (!config.SmartAddress.IsEqual(SmartComAddresses.Demo) && (string.IsNullOrWhiteSpace(config.SmartLive.Login) || string.IsNullOrWhiteSpace(config.SmartLive.Password)))
                        {
                            this.AddWarningLog(string.Format("{0}", "Не заданы логин и/или пароль SmartCom"));
                            OnNewStatusMessage("Не заданы логин и/или пароль SmartCom!");
                            return;
                        }

                        Connector = new SmartTrader();
                        ((SmartTrader)Connector).Address = config.SmartAddress;
                        ((SmartTrader)Connector).Version = SmartComVersions.V3;

                        if (config.SmartAddress.IsEqual(SmartComAddresses.Demo))
                        {
                            ((SmartTrader)Connector).Login = config.SmartDemo.Login;
                            ((SmartTrader)Connector).Password = config.SmartDemo.Password;
                        }
                        else
                        {
                            ((SmartTrader)Connector).Login = config.SmartLive.Login;
                            ((SmartTrader)Connector).Password = config.SmartLive.Password;
                        }

                        break;
                    case eConnectorType.Quik:
                        Connector = new QuikTrader
                        {
                            LuaLogin = config.LuaLogin,
                            LuaFixServerAddress = config.LuaFixServerAddress
                        };

                        break;
                    default:
                        break;
                }

                ConnectorType = config.ConnectorType;

                if (!LogManager.Sources.Contains(Connector))
                    LogManager.Sources.Add(Connector);

                OnConnectorCreated(Connector, config.ConnectorType);

                this.AddWarningLog(string.Format("Создан новый коннектор {0}", config.ConnectorType.ToString()));

                OnNewStatusMessage(string.Format("Создан новый коннектор {0}", config.ConnectorType.ToString()));

            }
            catch (Exception ex)
            {
                this.AddErrorLog(string.Format("{0} / {1}", ex.Message, ex.StackTrace));
                OnNewStatusMessage(ex.Message);
            }

        }

        private void OnConnect()
        {
            try
            {

                Connector.ReConnectionSettings.WorkingTime = ExchangeBoard.Forts.WorkingTime;

                Connector.Connected += () =>
                {
                    Application.Current.GuiSync(() => NotifyPropertyChanged("ConnectionState"));
                    _shootDown = false;
                    OnConnectionStateChanged(eConnectionState.StartConnect | eConnectionState.Connected);
                    OnNewStatusMessage("Соединение установлено!");
                    this.AddWarningLog(string.Format("{0}", "Соединение установлено!"));
                };

                Connector.ConnectionError += error => System.Windows.Application.Current.GuiAsync(() =>
                {
                    Application.Current.GuiSync(() => NotifyPropertyChanged("ConnectionState"));
                    this.AddErrorLog(error, LocalizedStrings.Str2959);
                    OnNewStatusMessage("Ошибка соединения!");
                //PlaySound();
            });

                Connector.Disconnected += () =>
                {
                    Application.Current.GuiAsync(() => NotifyPropertyChanged("ConnectionState"));
                    if (_shootDown)
                        Connector.Dispose();
                    OnConnectionStateChanged(eConnectionState.StopConnect | eConnectionState.Disconnected);
                    this.AddWarningLog(string.Format("{0}", "Соединение разорвано!"));
                    OnNewStatusMessage("Соединение разорвано!");
                };

                Connector.Restored += () =>
                {
                    OnNewStatusMessage("Соединение восстановлено!");
                    this.AddWarningLog(string.Format("{0}", "Соединение восстановлено!"));
                };

                Connector.Error += error =>
                {
                    this.AddErrorLog(error, LocalizedStrings.Str2955);
                    if (error.Message.Contains("Неправильный тип сообщения") || error.Message.Contains("Сообщение"))
                        return;

                    OnNewStatusMessage(error.Message);
                };

                Connector.LookupSecuritiesResult += securities =>
                {
                };

                Connector.MarketDataSubscriptionFailed += (security, type, error) =>
                {
                    MessageBox.Show(error.ToString(), LocalizedStrings.Str2956Params.Put(type, security), MessageBoxButton.OK, MessageBoxImage.Error);
                    this.AddWarningLog(string.Format("{0}", error.ToString()));
                };

                Connector.MarketTimeChanged += ts =>
                {

                    if (ts == TimeSpan.Zero)
                        return;

                    if (ts < MarketTimeDelay)
                    {
                        if ((ServerConnectionState & eConnectionState.Disconnected) == eConnectionState.Disconnected)
                        {
                            if ((ServerConnectionState & eConnectionState.LossConnect) == eConnectionState.LossConnect)
                            {
                                OnConnectionStateChanged(eConnectionState.Connected | eConnectionState.RestoreConnect);
                            }
                            else if ((ServerConnectionState & eConnectionState.StopConnect) == eConnectionState.StopConnect)
                            {
                                OnConnectionStateChanged(eConnectionState.Connected | eConnectionState.StartConnect);
                            }
                        }
                        else if ((ServerConnectionState & eConnectionState.Connected) == eConnectionState.Connected)
                        {
                            if ((ServerConnectionState & eConnectionState.StartConnect) == eConnectionState.StartConnect ||
                            (ServerConnectionState & eConnectionState.RestoreConnect) == eConnectionState.RestoreConnect)
                                OnConnectionStateChanged(eConnectionState.Connected);
                        }
                    }
                    else
                    {
                        if ((ServerConnectionState & eConnectionState.Connected) == eConnectionState.Connected)
                        {
                            if ((ServerConnectionState & eConnectionState.LossConnect) != eConnectionState.LossConnect)
                                OnConnectionStateChanged(eConnectionState.Disconnected | eConnectionState.LossConnect);
                        }
                    }

                //ServerConnected = ts != TimeSpan.Zero && ts < MarketTimeDelay && Connector.ConnectionState == ConnectionStates.Connected;


                var state = DateTimeOffset.Now.SessionState(Config.IsDemo);
                    if (SessionState != state)
                    {
                        SessionState = state;
                        OnSessionStateChanged(state);
                    }

                };

                Connector.ValuesChanged += OnValuesChanged;


                Connector.Connect();
            }
            catch (Exception ex)
            {
                this.AddErrorLog(string.Format("{0} / {1}", ex.Message, ex.StackTrace));
            }

        }

        private void OnValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> values, DateTimeOffset serverTime, DateTimeOffset localTime)
        {
            if (Chains.Any(c => c.UnderlyingAsset == security))
            {

            }
            else if (Chains.Any(c => c.Options.Any(o => o == security)))
            {

            }
        }

        public long GetLoaderId()
        {
            lock (_lock)
            {
                _currentLoaderId++;
                SaveId(_currentLoaderId);
            }
            return _currentLoaderId;
        }

        private void SaveId(long id)
        {
            //TODO
        } 


        #endregion

    }


}

