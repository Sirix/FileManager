using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System;
using System.Diagnostics;
using System.Windows;

namespace FileManager
{
    public class DirPanel : INotifyPropertyChanged
    {       
        public void FillOpenedDirectory()
        {
            this.OnPropertyChanged("OpenedDirectory");
        }
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private string _directory;
        public string OpenedDirectory
        {
            get
            {
                return _directory;
            }
            set
            {
                SanitizePath(ref value);

                try
                {
                    if (File.Exists(value))
                    {
                        Process.Start(value);
                    }
                    else if (Directory.Exists(value) || value == "Мой компьютер" || value == @"\\")
                    {
                        if (!value.EndsWith(Path.DirectorySeparatorChar.ToString()) && value != "Мой компьютер") value += Path.DirectorySeparatorChar;

                        this.CreateListing(value);
                        OnPropertyChanged("DirectoryListing");

                        _directory = value;
                        OnPropertyChanged("OpenedDirectory");

                        _historyInternal.Add(value);

                        if (value != @"Мой компьютер" && value != @"\\")
                        {
                            fsw.Path = value;
                            fsw.EnableRaisingEvents = true;
                        }
                        else
                            fsw.EnableRaisingEvents = false;
                    }
                }
                catch (Exception e)
                {
                    throw new FileSystemItemRunException(e.Message);
                }

            }
        }
        private bool _showHiddenObjects = true;
        public bool ShowHiddenObjects
        {
            get
            {
                return _showHiddenObjects;
            }
            set
            {
                _showHiddenObjects = value;
                OnPropertyChanged("ShowHiddenObjects");

                this.CreateListing(OpenedDirectory);
                OnPropertyChanged("DirectoryListing");
            }
        }

        private FileSystemWatcher fsw;


        private HistoryList _historyInternal;
        public IEnumerable<string> History { get { return _historyInternal.Reverse(); } }

        private string _informationLine;
        public string InformationLine
        {
            get { return _informationLine; }
            set
            {
                _informationLine = value;
                OnPropertyChanged("InformationLine");
            }
        }

        private string _localDisk;

        [Obsolete]
        public string LocalDisk
        {
            get { return _localDisk; }
            set
            {
                _localDisk = value;
                OnPropertyChanged("LocalDisk");
            }
        }
       
        public ObservableCollection<ExtFileSystemInfo> DirectoryListing { get; set; }

        private void SanitizePath(ref string value)
        {
            if (value.Length == 1 && char.IsLetter(value[0]))
                value += ":\\";
            else if (value.Length == 2 && char.IsLetter(value[0]) && value[1] == ':')
                value += "\\";
        }
        private void CreateListing(string OpenedDirectory)
        {
            int total = 0, folders = 0, files = 0;
            DirectoryInfo di;
            var tree = new List<ExtFileSystemInfo>();
            //if "\\", show "my computer" - enumerate all disks
            if (OpenedDirectory == @"\\" || OpenedDirectory == "Мой компьютер")
            {
                DirectoryListing.Clear();
                string[] disks = Environment.GetLogicalDrives();
                foreach (string disk in disks) tree.Add(new ExtFileSystemInfo(disk));
                total = disks.Length;
            }
            else
            {
                di = new DirectoryInfo(OpenedDirectory);

                var infos = di.GetFileSystemInfos();

                for (int i = 0; i < infos.Count(); i++)
                {
                    var data = infos.ElementAt(i);
                    ExtFileSystemInfo result = null;

                    //if not show hidden
                    if (!ShowHiddenObjects)
                    {
                        //if it isn't hidden, add it
                        if ((data.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                            result = new ExtFileSystemInfo(data.FullName);
                    }
                    else
                        result = new ExtFileSystemInfo(data.FullName);
                    if (result == null) continue;
                    if (result.IsDirectory) folders++;
                    if (result.IsFile) files++;

                    tree.Add(result);
                }

                DirectoryListing.Clear();
                //if local disk, set parent to my computer
                if (OpenedDirectory.Length == 3)
                    DirectoryListing.Add(new ExtFileSystemInfo(@"Мой компьютер") { IsSpecial = true });
                else
                {
                    if (di.Parent != null)
                        DirectoryListing.Add(new ExtFileSystemInfo(di.Parent.FullName) { IsSpecial = true });
                }
                total = infos.Length;
            }
            foreach (var e in tree.OrderByDescending(i => i.IsDirectory))
                DirectoryListing.Add(e);

            InformationLine = string.Format("Всего: {0} (Папок: {1}, Файлов: {2})", folders + files, folders, files);
        }
        public DirPanel(string directory)
        {
            DirectoryListing = new ObservableCollection<ExtFileSystemInfo>();
            fsw = new FileSystemWatcher();
            fsw.Created += fsw_Event;
            fsw.Deleted += fsw_Event;
            fsw.Changed += fsw_Event;
            fsw.Renamed += new RenamedEventHandler(fsw_Renamed);

            _historyInternal = new HistoryList(FileManager.Properties.Settings.Default.HistoryElementsCount);

            OpenedDirectory = directory;
        }

        void fsw_Renamed(object sender, RenamedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => DirectoryListing.First(i => i.FullName == e.OldFullPath).UpdateFileName(e.FullPath)));
        }

        void fsw_Event(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() =>
                                   {
                                       DirectoryListing.Add(new ExtFileSystemInfo(e.FullPath));
                                       var en = DirectoryListing.ToArray();
                                       Array.Sort(en,
                                                                new Comparison<ExtFileSystemInfo>((x, y) =>
                                                                {
                                                                    if (x.IsDirectory && !y.IsDirectory)
                                                                        return -1;
                                                                    if (x.IsDirectory && y.IsFile) return 1;

                                                                    return string.Compare(x.Name, y.Name);
                                                                }));
                                       int sorted = -1;
                                       for (int i = 0; i < en.Length; i++)
                                       {
                                           if(en[i].Name == e.Name || en[i].Name == string.Format("[{0}]", e.Name))
                                           {
                                               sorted=i;
                                               break;
                                           }
                                       }
                                       DirectoryListing.Move(DirectoryListing.Count - 1, sorted);
                                   }));
            }
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => DirectoryListing.Remove(DirectoryListing.First(i => i.FullName == e.FullPath))));
            }
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {

            }
        }
        internal void Refresh()
        {
            this.CreateListing(OpenedDirectory);
        }

        internal void CreateObject(CreationType t, string name)
        {
            if (t == CreationType.File)
                File.Create(Path.Combine(OpenedDirectory, name));
            else
                Directory.CreateDirectory(Path.Combine(OpenedDirectory, name));
        }
    }
}