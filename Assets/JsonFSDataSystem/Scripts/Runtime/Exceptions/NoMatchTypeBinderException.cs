using System;

namespace Ca2didi.JsonFSDataSystem.Exceptions
{
    public class NoMatchTypeBinderException : Exception
    {
        public string TypeString { get; }

        internal NoMatchTypeBinderException(string typeString) : base(
            $"Type string '{typeString}' is not defined in program. Maybe you should add this string to binder.")
        {
            TypeString = typeString;
        }
    }
}