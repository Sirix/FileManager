using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

namespace FileManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<DirPanel> panels = new List<DirPanel>();

        public MainWindow()
        {
            InitializeComponent();
            btnCreateNewPanel_Click(null, null);
            btnCreateNewPanel_Click(null, null);
        }

        private void btnCreateNewPanel_Click(object sender, RoutedEventArgs e)
        {
            DirPanel dpContext = new DirPanel(@"\\");

            var panel = new DirectoryPanel(dpContext);
            panel.ClosingRequested += new DirectoryPanelNeedClosing(panel_ClosingIsRequested);
            mainPanel.Children.Add(panel);

            //bind newly created control height property to actualheight property of master stackpanel
            Binding b = new Binding("ActualHeight");
            b.Source = mainPanel;
            panel.SetBinding(FrameworkElement.HeightProperty, b);
        }

        void panel_ClosingIsRequested(DirectoryPanel instance)
        {
            mainPanel.Children.Remove(instance);
        }
    }
}

