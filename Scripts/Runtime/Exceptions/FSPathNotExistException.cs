using System;
using Ca2didi.JsonFSDataSystem.FS;

namespace Ca2didi.JsonFSDataSystem.Exceptions
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