using System;

namespace FileManager
{
    /// <summary>
    /// Represents errors that occur during launching filesystem item - file or directory, 
    /// Typo, illegal or wrong path, unsufficient priviligeis and many other.
    /// </summary>
    [Serializable]
    public class FileSystemItemRunException : Exception
    {
        public FileSystemItemRunException() { }
        public FileSystemItemRunException(string message) : base(message) { }
        public FileSystemItemRunException(string message, Exception inner) : base(message, inner) { }
        protected FileSystemItemRunException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public bool Silent { get; set; }
    }
}
