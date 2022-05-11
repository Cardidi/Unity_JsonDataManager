using System;
using xyz.ca2didi.Unity.JsonFSDataSystem.FS;

namespace xyz.ca2didi.Unity.JsonFSDataSystem.Exceptions
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