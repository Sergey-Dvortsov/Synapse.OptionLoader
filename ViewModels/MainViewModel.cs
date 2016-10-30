namespace Synapse.OptionLoader
{
    using Synapse.Common;
    using Synapse.Xaml;
    using Ecng.Xaml;
    using StockSharp.Messages;
    using System.Windows;
    using System.Windows.Threading;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Модель представления главного окна
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private LoaderRoot _root;

        private Dispatcher _dispatcher;

        public MainViewModel()
        {
            _root = LoaderRoot.GetInstance();

            _dispatcher = Application.Current.Dispatcher;

            ConnectCommand = new DelegateCommand(Connect, CanConnect);
            SettingsCommand = new DelegateCommand(ShowSettings, CanShowSettings);

            _root.NewStatusMessage += message =>
            {
                StatusMessage = message;
            };

            _root.ConnectorCreated += (connector, type) =>
            {
                _root.Connector.Connected += () =>
                {
                    ConnectionState = ConnectionStates.Connected;
                };

                _root.Connector.Disconnected += () =>
                {
                    ConnectionState = ConnectionStates.Disconnected;
                };

                NotifyPropertyChanged("ConnectorType");
            };

            _root.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "ServerConnectionState":
                        NotifyPropertyChanged("ServerConnectionState");
                        break;
                    case "ConnectorType":
                        NotifyPropertyChanged("ConnectorType");
                        break;
                    case "SessionState":
                        NotifyPropertyChanged("SessionState");
                        break;
                    default:
                        break;
                }
            };

        }

        private ConnectionStates _connectionState;
        public ConnectionStates ConnectionState
        {
            get { return _connectionState; }
            set
            {
                _connectionState = value;
                _dispatcher.GuiAsync(() => NotifyPropertyChanged());
            }
        }

        public eConnectorType ConnectorType
        {
            get { return _root.ConnectorType; }
        }

        public eConnectionState ServerConnectionState
        {
            get { return _root.ServerConnectionState; }
        }

        public eSessionState SessionState
        {
            get { return _root.SessionState; }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            set
            {
                _statusMessage = value;
                _dispatcher.GuiAsync(() => NotifyPropertyChanged());
            }
        }

        #region Commands

        public DelegateCommand ConnectCommand { get; set; }

        private void Connect(object obj)
        {
            if (_root.Connector.ConnectionState != ConnectionStates.Connected)
            {
                _root.Connect();
            }
            else
            {
                _root.Disconnect();
            }
        }

        private bool CanConnect(object obj)
        {
            return _root.Connector != null;
        }

        public DelegateCommand SettingsCommand { get; set; }

        private void ShowSettings(object obj)
        {
            var dlg = new ConfigurationWindow(_root.Config);
            if (dlg.ShowDialog().Value)
            {
                _root.Config = (Configuration)dlg.DataContext;
                _root.Save();
            }
        }

        private bool CanShowSettings(object obj)
        {
            return true;
        }

        #endregion

        public void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _root.ShootDown();
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            _root.Load();
        }


    }
}
