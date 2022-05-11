using System;
using xyz.ca2didi.Unity.JsonFSDataSystem.FS;

namespace xyz.ca2didi.Unity.JsonFSDataSystem.Exceptions
{
    public class FSPathNotExistException : Exception
    {
        public FSPath Path { get; private set; }
        
        internal FSPathNotExistException(FSPath path) : base($"Can not find path '{path.FullPath}' !")
        {
            Path = path;
        }
    }
}