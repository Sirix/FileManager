using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using System.Windows.Media.Animation;
using System.Collections;
using VBFileIO = Microsoft.VisualBasic.FileIO;

namespace FileManager
{
    /// <summary>
    /// Invoked when specified DirectoryPanel instance requested an exit
    /// </summary>
    /// <param name="instance">DirectoryPanel instance</param>
    public delegate void DirectoryPanelNeedClosing(DirectoryPanel instance);

    /// <summary>
    /// Interaction logic for DirectoryPanel.xaml
    /// </summary>
    public partial class DirectoryPanel : UserControl
    {
        private List<ExtFileSystemInfo> objSelectedBySpaceBar = new List<ExtFileSystemInfo>();

        public event DirectoryPanelNeedClosing ClosingRequested;

        private static Point InvalidPoint = new Point(-1, -1);
        private string currentLocation;

        public DirectoryPanel(DirPanel dpContext)
        {
            this.DataContext = dpContext;

            InitializeComponent();
            InitializeDataContext();

            DirectoryList.PreviewKeyDown += new KeyEventHandler(DirectoryList_KeyDown);
            ((INotifyCollectionChanged)DirectoryList.Items).CollectionChanged += new NotifyCollectionChangedEventHandler(DirectoryPanel_CollectionChanged);
        }

        /// <summary>
        /// This function protects code from bug in .NET 3.5:
        /// On binding source to target and on update source property value in setter method,
        /// it wouldn't updated at target.
        /// This bug is fixed since .NET 4.0 CTP
        /// </summary>
        private void InitializeDataContext()
        {
            var dc = DataContext as DirPanel;
            dc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "OpenedDirectory")
                {
                    this.tbLocation.Text = dc.OpenedDirectory;
                    currentLocation = tbLocation.Text;
                    objSelectedBySpaceBar.Clear();
                }
                if (e.PropertyName == "InformationLine")
                { this.directorySum.Text = dc.InformationLine; }
            };
            dc.FillOpenedDirectory();
            currentLocation = dc.OpenedDirectory;
        }

        //scroll listbox to first element after binding, protect from bug with non-visible items instantly after binding complete
        void DirectoryPanel_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DirectoryList.Items.Count != 0)
                DirectoryList.ScrollIntoView(DirectoryList.Items[0]);
        }

        void tbLocation_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //no new value entered
                if (tbLocation.Text == currentLocation) return;

                if (string.IsNullOrEmpty(tbLocation.Text.Trim()))
                {
                    tbLocation.Text = currentLocation;
                    return;
                }
                Execute(tbLocation.Text);

                this.DirectoryList.Focus();
            }
        }

        /// <summary>
        /// Occurs on keypresses at directory list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DirectoryList_KeyDown(object sender, KeyEventArgs e)
        {
            var dcPanel = this.DataContext as DirPanel;

            if (e.Key == Key.F4)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    this.CreateObject(CreationType.File);
                    return;
                }
                if (this.DirectoryList.SelectedItem != null)
                {
                    var data = (this.DirectoryList.SelectedItem as ExtFileSystemInfo);
                    if (!data.IsDirectory) Process.Start("notepad.exe", data.FullName);
                }
            }
            if (e.Key == Key.F7)
            {
                this.CreateObject(CreationType.Folder);
            }
            if (e.Key == Key.Enter)
            {
                if (this.DirectoryList.SelectedItem != null)
                {
                    var data = (this.DirectoryList.SelectedItem as ExtFileSystemInfo);
                    this.Execute(data.FullName);
                }
            }
            if (e.Key == Key.Delete || e.Key == Key.F8)
            {
                DeleteFiles();
            }
            if (e.Key == Key.Space)
            {
                var selected = DirectoryList.SelectedItem as ExtFileSystemInfo;
                if (selected.IsSpecial || selected.IsLocalDisk) return;

                if (!objSelectedBySpaceBar.Contains(selected))
                {
                    (this.DirectoryList.ItemContainerGenerator.ContainerFromItem(selected) as ListBoxItem).Foreground = Brushes.Red;
                    objSelectedBySpaceBar.Add(selected);
                }
                else
                {
                    (this.DirectoryList.ItemContainerGenerator.ContainerFromItem(selected) as ListBoxItem).Foreground = Brushes.Black;
                    objSelectedBySpaceBar.Remove(selected);
                }
                directorySum.Text = objSelectedBySpaceBar.Count > 0 ?
                    string.Format("Выбрано {0} элемент(ов), общий размер: {1} ", objSelectedBySpaceBar.Count, ExtFileSystemInfo.BuildLengthString(objSelectedBySpaceBar.Sum(i => i.Size)))
                    : dcPanel.InformationLine;
            }
        }

        private void DeleteFiles()
        {
            List<ExtFileSystemInfo> toDelete = new List<ExtFileSystemInfo>();
            string value = "";
            foreach (var i in DirectoryList.SelectedItems)
            {
                var item = i as ExtFileSystemInfo;
                if (item.IsSpecial || item.IsLocalDisk) continue;

                value += item.Name + "\n";
                toDelete.Add(item);
            }
            if (MessageBox.Show("Удалить эти файл(ы)/папки? \n" + value, "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            foreach (ExtFileSystemInfo i in toDelete)
            {
                try
                {
                    i.Delete();
                }
                catch (Exception e)
                {
                    this.DisplayError(string.Format("Ошибка удаления файла/папки \n{0} \nПричина: {1}", i.Name, e.Message), "Удаление");
                }
            }
        }

        private void ToggleSelectedItemColor()
        {
            if (this.DirectoryList.SelectedItems != null)
            {
                foreach (var i in this.DirectoryList.SelectedItems)
                {
                    (this.DirectoryList.ItemContainerGenerator.ContainerFromItem(i) as ListBoxItem).Foreground = Brushes.Red;
                }
            }
        }

        private void Execute(string path)
        {
            try
            {
                tbLocation.Text = currentLocation;
                (this.DataContext as DirPanel).OpenedDirectory = path;
            }
            catch (FileSystemItemRunException e)
            {
                DisplayError(e.Message);
            }
        }

        private void DisplayError(string message, string windowTitle = "Ошибка")
        {
            this.BeginStoryboard((Storyboard)this.FindResource("storyboard_Error"));
            MessageBox.Show(message, windowTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void DirectoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.DirectoryList.SelectedItem == null) return;

            var data = (this.DirectoryList.SelectedItem as ExtFileSystemInfo);
            this.Execute(data.FullName);
        }

        private void HistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var location = (sender as MenuItem).DataContext as string;

            this.Execute(location);
        }

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var dc = mi.DataContext as ExtFileSystemInfo;

            switch (mi.Name)
            {
                case "miOptions_showHidden":
                    mi.IsChecked = !mi.IsChecked;
                    break;

                case "miOptions_refresh":
                    (this.DataContext as DirPanel).Refresh();
                    break;

                case "miList_createFolder":
                    this.CreateObject(CreationType.Folder); 
                    break;

                case "miList_createFile":
                    this.CreateObject(CreationType.File); 
                    break;

                case "miFile_copy":
                    var list = new List<ExtFileSystemInfo>();
                    list.AddRange(DirectoryList.SelectedItems.Cast<ExtFileSystemInfo>());

                    var d = (from i in list
                             where i.IsSpecial == false && i.IsLocalDisk == false
                             select i).ToList();

                    CopyPasteApplicationStorage.CopyPasteData =
                        new CopyPasteWrapper()
                        {
                            Files = d,
                            FromContext = this.DataContext as DirPanel
                        };
                    break;

                case "miFile_paste":
                    if (dc.IsFile || dc.IsSpecial) return;
                    this.ProcessPaste(dc.FullName);
                    break;
                
                case "miList_paste":
                    this.ProcessPaste(tbLocation.Text);
                    break;

                case "miFile_delete":
                    this.DeleteFiles();
                    break;

                case "miFile_properties":
                    if (dc.IsSpecial) return;
                    FileManager.Interop.PropertiesWindowHandler.ShowFileProperties(dc.FullName);
                    break;

                default:
                    return;
            }
        }

        private void ProcessPaste(string targetPath)
        {
            if (CopyPasteApplicationStorage.CopyPasteData == null) return;

            string m = "";
            int count = 0;
            foreach (var i in CopyPasteApplicationStorage.CopyPasteData.Files)
            {
                if (++count > 3) { m += "...\n"; break; }

                m += i.Name + "\n";
            }
            if (MessageBox.Show(
                string.Format("Скопировать следующие папки/файлы ({0}) \n{1} в {2} ?", CopyPasteApplicationStorage.CopyPasteData.Files.Count(), m, targetPath),
                "Копирование",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.No) return;

            foreach (var item in CopyPasteApplicationStorage.CopyPasteData.Files)
                if (!this.CopyObject(item, targetPath)) break;

            (this.DataContext as DirPanel).Refresh();
        }

        private bool CopyObject(ExtFileSystemInfo item, string targetPath)
        {
            string sourceName = item.FullName;
            string name = "";
            if (item.IsDirectory) name = new DirectoryInfo(item.FullName).Name;
            if (item.IsFile) name = Path.GetFileName(item.FullName);

            string targetName = Path.Combine(targetPath, name);

            //prevent from self-to-self copying
            bool copyAllow = !sourceName.Equals(targetName, StringComparison.OrdinalIgnoreCase);

            if (!copyAllow)
            {
                if (FileManager.Properties.Settings.Default.OnSemiCopy == "ContinueWithNext")
                {
                    MessageBox.Show("Попытка копирования объекта в самого себя!\n" + item.FullName, "Копирование", MessageBoxButton.OK, MessageBoxImage.Error);
                    return true;
                }
                else
                    targetName = ExtFileSystemInfo.CreateNewFileName(targetPath, item, name);
            }

            try
            {
                if (item.IsDirectory)
                    VBFileIO.FileSystem.CopyDirectory(sourceName, targetName, VBFileIO.UIOption.AllDialogs, VBFileIO.UICancelOption.DoNothing);
                else
                    VBFileIO.FileSystem.CopyFile(sourceName, targetName, VBFileIO.UIOption.AllDialogs, VBFileIO.UICancelOption.DoNothing);
            }
            catch (Exception e)
            {
                if (MessageBox.Show(string.Format("Произошла ошибка при копировании \n{0} \nв{1}\n Информация об ошибке:\n{2} \nПродолжить со следующими файлами?", item.Name, targetPath, e.Message),
                        "Копирование",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.No)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Call datacontext to create file or folder
        /// </summary>
        /// <param name="t">Type of which filesystem item will be created</param>
        private void CreateObject(CreationType t)
        {
            var dlg = new CreateFileWindow(t);
            dlg.ShowDialog();
            dlg.Owner = Application.Current.MainWindow;

            if (dlg.DialogResult.HasValue && dlg.DialogResult.Value)
                (this.DataContext as DirPanel).CreateObject(t, dlg.Result);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            // call mainwindow to close this instance
            if (ClosingRequested != null)
                ClosingRequested(this);
        }

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            menuHistory.PlacementTarget = this;
            menuHistory.IsOpen = true;
        }

        private void DirectoryList_Drop(object sender, DragEventArgs e)
        {
            //copyToFSItem can be null when user dropped item on clean space of directory list
            ExtFileSystemInfo copyToFSItem = this.GetDataFromListBox(this.DirectoryList, e.GetPosition(this.DirectoryList)) as ExtFileSystemInfo;

            ExtFileSystemInfo data = e.Data.GetData(typeof(ExtFileSystemInfo)) as ExtFileSystemInfo;

            if (data.IsSpecial /* || copyToFSItem.IsSpecial*/) return;
            //prevent from one click false drop event
            if (copyToFSItem == data) return;

            string copyToPath;
            if (copyToFSItem == null || copyToFSItem.IsDirectory == false)
                copyToPath = this.tbLocation.Text;
            else
                copyToPath = copyToFSItem.FullName;

            if (MessageBox.Show(
                string.Format("Скопировать папку/файл \n{0} в {1} ?", data.Name, copyToPath),
                "Копирование",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.No) return;

            this.CopyObject(data, copyToPath);
        }

        Point startPoint;
        //since i need two other actions on one-click and double-click, i use this schema for route events
        private void DirectoryList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { this.DirectoryList_MouseDoubleClick(sender, e); return; }

            startPoint = e.GetPosition(this.DirectoryList);
        }

        private void DirectoryList_MouseMove(object sender, MouseEventArgs e)
        {
            if (startPoint == DirectoryPanel.InvalidPoint) return;

            // Get the current mouse position
            Point mousePos = e.GetPosition(this.DirectoryList);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {

                object data = GetDataFromListBox(this.DirectoryList, startPoint);
                if (data != null)
                    DragDrop.DoDragDrop(this.DirectoryList, data, DragDropEffects.Copy);
            }
        }

        #region GetDataFromListBox(ListBox,Point)
        private object GetDataFromListBox(ListBox source, Point point)
        {
            UIElement element = source.InputHitTest(point) as UIElement;
            if (element != null)
            {
                object data = DependencyProperty.UnsetValue;
                while (data == DependencyProperty.UnsetValue)
                {
                    data = source.ItemContainerGenerator.ItemFromContainer(element);
                    if (data == DependencyProperty.UnsetValue)
                    {
                        element = VisualTreeHelper.GetParent(element) as UIElement;
                    }
                    if (element == source)
                    {
                        return null;
                    }
                }
                if (data != DependencyProperty.UnsetValue)
                {
                    return data;
                }
            }
            return null;
        }

        #endregion

        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            cmOptions.PlacementTarget = this;
            cmOptions.IsOpen = true;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            startPoint = DirectoryPanel.InvalidPoint;
        }

        private void DirectoryList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            startPoint = DirectoryPanel.InvalidPoint;

            //show item context menu, not list menu
            if (e.OriginalSource is Border)
            {
                var pt = Interop.MouseUtilities.CorrectGetPosition(DirectoryList);
                var item = this.GetDataFromListBox(DirectoryList, pt);
                if (item == null) return;

                var itemContainer = this.DirectoryList.ItemContainerGenerator.ContainerFromItem(item);

                //Find content presenter in this itemContainer
                ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(itemContainer);

                // Finding textBlock from the DataTemplate that is set on that ContentPresenter
                DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                var cmProp = (ContextMenu)myDataTemplate.FindName("cmProperties", myContentPresenter);

                cmProp.DataContext = item;
                cmProp.PlacementTarget = this;
                cmProp.IsOpen = true;

                e.Handled = true;
            }
        }
        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}