using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Synapse.OptionLoader
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Trace.WriteLine(e.Exception.Message + ", " + e.Exception.StackTrace);
            e.Handled = true;
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Trace.WriteLine(ex.Message + ", " + ex.StackTrace);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {

            var logFile = "UnhandledException.log";

            Trace.Listeners.Add(new TextWriterTraceListener(File.CreateText(logFile)));
            Trace.AutoFlush = true;

            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += OnUnhandledException;
            try
            {
                //Assembly assembly = Assembly.GetAssembly(typeof(App));
                //var version = assembly.GetName().Version;
                // Инициализируем корневой класс

                var root = LoaderRoot.GetInstance();

                root.Init();

                Window win = new MainWindow();
                this.MainWindow = win;
                win.Show();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("{0} / {1}", ex.Message, ex.StackTrace));
            }
        }
    }
}
