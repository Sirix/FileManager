using System;
using System.Windows;
using System.Text;

namespace FileManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#endif
            this.StartupUri = new Uri("pack://application:,,,/FileManager;component/MainWindow.xaml");
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (e.ExceptionObject as Exception);
            StringBuilder sb = new StringBuilder();
            string date = DateTime.Now.ToLongTimeString();
            string filename = "crash-" + date.Replace(':', '_') + ".log";
            sb.AppendLine("Crash report");
            sb.AppendFormat("Date and time: {0}\n", date);
            sb.AppendLine("==================================================");
            sb.AppendLine(ex.Message);
            sb.AppendLine("==================================================");
            sb.AppendFormat("Opened dir panels: {0}", (this.MainWindow as MainWindow).mainPanel.Children.Count);
            sb.AppendLine("========\nStacktrace\n=======");
            sb.AppendLine(ex.StackTrace);
            System.IO.File.WriteAllText(filename, sb.ToString());
            MessageBox.Show("Произошла ошибка при работе программы.\n Данные об ошибке собраны в файл " + filename, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.ExitCode = 0;
        }
    }
}
