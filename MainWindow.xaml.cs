using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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

#if DEBUG
            ((mainPanel.Children[0] as DirectoryPanel).DataContext as FileManager.DirPanel).OpenedDirectory = @"e:\prodinner\";
#endif
        }

        private void btnCreateNewPanel_Click(object sender, RoutedEventArgs e)
        {
            DirPanel dpContext = new DirPanel(@"\\");

            var panel = new DirectoryPanel(dpContext);
            panel.ClosingRequested += new DirectoryPanelNeedClosing(panel_ClosingIsRequested);
            mainPanel.Children.Add(panel);

            //bind newly created control height property to actualheight property of master stackpanel
            Binding b = new Binding("ActualHeight") {Source = mainPanel};
            panel.SetBinding(FrameworkElement.HeightProperty, b);
        }

        void panel_ClosingIsRequested(DirectoryPanel instance)
        {
            mainPanel.Children.Remove(instance);
        }

        private void ScrollViewer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.OriginalSource is ListBoxItem) return;

            foreach (DirectoryPanel item in mainPanel.Children)
            {
                //get mouse position relative to directorypanel
                var pt = FileManager.Interop.MouseUtilities.CorrectGetPosition(item);
                if (pt.X <= item.ActualWidth)
                {
                    //call dp instance to handle key event
                    item.DirectoryList_KeyDown(null, e);
                    break;
                }
            }
        }
    }
}

