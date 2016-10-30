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
    public class LoadSetupViewModel : BaseViewModel
    {
        private LoaderRoot _root;

        public LoadSetupViewModel()
        {
            _root = LoaderRoot.GetInstance();
            ConnectCommand = new DelegateCommand(Connect, CanConnect);
            SettingsCommand = new DelegateCommand(ShowSettings, CanShowSettings);
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
            //
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
           //
        }


    }
}
