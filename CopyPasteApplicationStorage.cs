using System.Collections.Generic;

namespace FileManager
{
    public class CopyPasteWrapper
    {
        public DirPanel FromContext { get; set; }
        public IEnumerable<ExtFileSystemInfo> Files { get; set; }
    }

    public static class CopyPasteApplicationStorage
    {
        public static CopyPasteWrapper CopyPasteData { get; set; }
    }
}
