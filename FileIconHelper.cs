using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.IO;
using System.Drawing;

namespace FileManager
{
    class FileIconHelper : Dictionary<string, ImageSource>
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        class Win32
        {
            public const uint SHGFI_ICON = 0x100;
            public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
            public const uint SHGFI_SMALLICON = 0x1; // 'Small icon

            [DllImport("shell32.dll")]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            [DllImport("User32.dll")]
            public static extern int DestroyIcon(IntPtr hIcon);
        }

        private static FileIconHelper _instance;
        public static FileIconHelper get()
        {
            if (_instance == null) _instance = new FileIconHelper();

            return _instance;
        }

        internal ImageSource Find(string file)
        {
            string extension = Path.GetExtension(file);
            if (this.ContainsKey(extension) && extension != ".exe" && extension != ".ico") return this[extension];
            ImageSource image = new System.Windows.Media.Imaging.BitmapImage();

            //if (!FileManager.Properties.Settings.Default.Load_MDF_MDS_Icon)
                //In this cases program fails with StackOverflowException in mscorlib.dll. I can't find any solution other it :(
                if (extension == ".mdf" || extension == ".mds") return new ImageDrawing().ImageSource;

            try
            {
                IntPtr hImgSmall;    //the handle to the system image list
                SHFILEINFO shinfo = new SHFILEINFO();

                hImgSmall = Win32.SHGetFileInfo(file, 0, ref shinfo,
                                                   (uint)Marshal.SizeOf(shinfo),
                                                    Win32.SHGFI_ICON |
                                                    Win32.SHGFI_SMALLICON);

                var i = Icon.FromHandle(shinfo.hIcon);
                image = i.ToImageSource();
                Win32.DestroyIcon(shinfo.hIcon);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }

            //not save icons for exe files and directories
            if (extension != ".exe" && extension != "" && extension != ".ico")
                this.Add(extension, image);

            return image;
        }
    }
}