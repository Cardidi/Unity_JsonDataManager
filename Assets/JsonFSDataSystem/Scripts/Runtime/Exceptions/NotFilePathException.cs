using System;
using Ca2didi.JsonFSDataSystem.FS;

namespace Ca2didi.JsonFSDataSystem.Exceptions
{
    public class NotFilePathException : Exception
    {
        public FSPath Path { get; }

        internal NotFilePathException(FSPath path) : base($"Path '{path}' is not used for a file!")
        {
            Path = path;
        }
    }
}