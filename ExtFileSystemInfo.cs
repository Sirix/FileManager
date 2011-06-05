using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace FileManager
{
    public class ExtFileSystemInfo : FileSystemInfo, INotifyPropertyChanged
    {
        public static double Byte = 1024;
        public static double Kilobyte = Math.Pow(1024, 2);
        public static double Megabyte = Math.Pow(1024, 3);
        public static double Gigabyte = Math.Pow(1024, 4);


        public ExtFileSystemInfo() { }
        public ExtFileSystemInfo(string path)
        {
            base.FullPath = path;
            base.Refresh();

            IsDirectory = Directory.Exists(path);

            Icon = FileIconHelper.get().Find(base.FullPath);

            IsLocalDisk = new Regex(@"^[a-zA-Z]:\\$").Match(this.FullPath).Success;

            IsFile = !(IsLocalDisk || IsDirectory);
        }
        public bool IsFile { get; private set; }
        /// <summary>
        /// True, if this ExtFileSystemInfo object refers to local disk - C:\, D:\, etc; otherwise, false.
        /// This property is read-only.
        /// </summary>
        public bool IsLocalDisk { get; private set; }

        /// <summary>
        /// True, if this ExtFileSystemInfo object used for special object - directory [..], which refer to owner dir; otherwise, false.
        /// </summary>
        public bool IsSpecial { get; set; }

        public override void Delete()
        {
            if (IsDirectory)
            {
                new DirectoryInfo(this.FullPath).Delete(true);
            }
            else
                new FileInfo(this.FullPath).Delete();
        }
        public override string FullName
        {
            get
            {
                return base.FullPath;
            }
        }
        public override bool Exists
        {
            get { return true; }
        }

        public double Size
        {
            get
            {
                try
                {
                    if (IsDirectory)
                        return this.GetDirectorySize(new DirectoryInfo(this.FullPath));
                    else
                        return (double)(new FileInfo(this.FullPath).Length);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        public override string Name
        {
            get
            {
                if (IsSpecial) return "[..]";

                if (IsLocalDisk) return this.FullPath;

                if (IsDirectory)
                    return string.Format("[{0}]", Path.GetFileName(this.FullPath));
                else
                    return Path.GetFileName(this.FullPath);
            }
        }

        public bool IsDirectory { get; private set; }
        public ImageSource Icon { get; private set; }
        public override string ToString()
        {
            return this.Name;
        }
        /// <summary>
        /// Creates string represenation for file length in bytes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string BuildLengthString(double value)
        {
            if (value <= ExtFileSystemInfo.Byte) return string.Format("{0:F2} bytes", value);
            if (value <= ExtFileSystemInfo.Kilobyte) return string.Format("{0:F2} KB", value / 1024);
            if (value <= ExtFileSystemInfo.Megabyte) return string.Format("{0:F2} MB", value / 1024 / 1024);
            if (value <= ExtFileSystemInfo.Gigabyte) return string.Format("{0:F2} GB", value / 1024 / 1024 / 1024);

            return "";
        }

        public double GetDirectorySize(DirectoryInfo directory)
        {
            double sum = 0;
            foreach (FileInfo file in directory.GetFiles())
                sum += file.Length;

            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                sum += GetDirectorySize(subDirectory);

            return sum;
        }

        /// <summary>
        /// Creates new filename for already existing file or folder.
        /// </summary>
        /// <param name="targetPath">Path, where new file or folder will be located</param>
        /// <param name="item">ExtFileSystemInfo object which describes file or folder</param>
        /// <param name="name">Current file or folder name</param>
        /// <returns>Full path with filename, which doesn't exist on disk</returns>
        public static string CreateNewFileName(string targetPath, ExtFileSystemInfo item, string name)
        {
            string targetName = Path.Combine(targetPath, name);
            int i = 2;
            Func<string, bool> caller;
            if (item.IsFile)
            {
                caller = new Func<string, bool>(File.Exists);
            }
            else
                caller = new Func<string, bool>(Directory.Exists);

            while (caller(targetName))
                targetName = Path.Combine(targetPath, name + string.Format("({0})", i++));

            return targetName;
        }


        internal void UpdateFileName(string path)
        {
            base.FullPath = path;
            base.Refresh();

            Icon = FileIconHelper.get().Find(base.FullPath);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                PropertyChanged(this, new PropertyChangedEventArgs("Icon"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
