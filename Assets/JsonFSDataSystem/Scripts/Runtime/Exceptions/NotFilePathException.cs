using System;
using xyz.ca2didi.Unity.JsonFSDataSystem.FS;

namespace xyz.ca2didi.Unity.JsonFSDataSystem.Exceptions
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