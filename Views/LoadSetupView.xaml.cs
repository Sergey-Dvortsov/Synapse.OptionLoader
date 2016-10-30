namespace Synapse.OptionLoader
{
    using System.Windows;

    /// <summary>
    /// Логика взаимодействия для LoadSetupView.xaml
    /// </summary>
    public partial class LoadSetupView : Window
    {
        public LoadSetupView()
        {
            InitializeComponent();
            DataContext = new LoadSetupViewModel();
        }
    }
}
