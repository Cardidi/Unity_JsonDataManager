using System;
using Ca2didi.JsonFSDataSystem.FS;

namespace Ca2didi.JsonFSDataSystem.Exceptions
{
    public class NotFolderPathException : Exception
    {
        public FSPath Path { get; }

        internal NotFolderPathException(FSPath path) : base($"Path '{path}' is not used for a folder!")
        {
            Path = path;
        }
    }
}