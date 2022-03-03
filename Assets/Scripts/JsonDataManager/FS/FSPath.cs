using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace xyz.ca2didi.Unity.JsonDataManager.FS
{
    
    /// <summary>
    /// The File System Path in JsonDataManager to locate a data as similar as query a file in C#.
    /// </summary>
    public struct FSPath
    {

        private static Regex
            generator_container = new Regex(@"^(?<container>[\w\d\^\W]+)://(?<url>[\w\d\-_/.]+)$"),
            generator_path = new Regex(@"/(?<dirName>[\w\d\-_\.]+)"),
            generator_fileName = new Regex(@"^(?<fileName>[\w\d-_]*)\.(?<fileType>[\w\d]*)$");
        
        /// <summary>
        /// Create a new FSPath by path string (Full or short)
        /// </summary>
        /// <param name="path">Path String</param>
        public FSPath(string path)
        {
            // Safety
            path = path.Trim();
            if (String.IsNullOrWhiteSpace(path))
                throw new Exception("Invalid Format");

            // Init
            ContainerName = "current";
            DirectoryName = Array.Empty<string>();
            FileName = FileType = "";
            _cachedShortPath = _cachedFullPath = "";
            
            var container = generator_container.Match(path);
            var dics = new List<string>();
            
            // Match container name
            if (container.Success)
            {
                ContainerName = container.Groups["container"].Value.ToLower();
                
                // Match path
                var p = generator_path.Matches(container.Groups["url"].Value);
                for (var i = 0; i < p.Count; i++)
                {
                    var dir = p[i].Groups["dirName"].Value;
                    
                    // Match file name
                    var f = generator_fileName.Match(dir);
                    if (f.Success)
                    {
                        // Not at the last item in path : error
                        if (i != p.Count - 1)
                            throw new Exception("Format invalid");

                        FileName = f.Groups["fileName"].Value;
                        FileType = f.Groups["fileType"].Value;
                    }
                    else
                    {
                        dics.Add(dir);
                    }
                }
            }
            else
            {
                if (path != "/")
                {
                    // Match path
                    var p = generator_path.Matches(path);
                    if (p.Count == 0)
                        throw new Exception("Invalid Format");
                    
                    for (var i = 0; i < p.Count; i++)
                    {
                        var dir = p[i].Groups["dirName"].Value;
                    
                        // Match file name
                        var f = generator_fileName.Match(dir);
                        if (f.Success)
                        {
                            // Not at the last item in path : error
                            if (i != p.Count - 1)
                                throw new Exception("Format invalid");

                            FileName = f.Groups["fileName"].Value.Trim();
                            FileType = f.Groups["fileType"].Value.Trim();
                            if (string.IsNullOrEmpty(FileName))
                                throw new Exception("Format invalid");
                        }
                        else
                        {
                            dics.Add(dir);
                        }
                    }
                }
            }
                
            DirectoryName = dics.ToArray();
        }

        private FSPath(FSPath child, int previous)
        {
            if (child.Equals(default))
                throw new NullReferenceException(nameof(child));
            
            if (previous < 1)
                throw new ArgumentOutOfRangeException(nameof(previous));

            if (!string.IsNullOrEmpty(child.FileType))
                previous -= 1;

            FileName = FileType = "";
            _cachedShortPath = _cachedFullPath = "";
            ContainerName = child.ContainerName;
            
            List<string> ary = new List<string>();
            for (var i = 0; i < child.DirectoryName.Length - previous; i++)
            {
                ary.Add(child.DirectoryName[i]);
            }

            DirectoryName = ary.ToArray();
        }

        private FSPath(FSPath parent, string location)
        {
            
            if (parent.Equals(default))
                throw new NullReferenceException(nameof(parent));

            if (string.IsNullOrEmpty(location))
                throw new NullReferenceException(nameof(location));

            if (string.IsNullOrEmpty(parent.FileType))
                throw new InvalidOperationException("Parent is a file, not a directory!");

            _cachedShortPath = _cachedFullPath = "";
            FileName = FileType = "";
            
            var p = generator_path.Matches(location);
            if (p.Count == 0)
                throw new Exception("Invalid Format");
                    
            var dics = new List<string>(parent.DirectoryName);
            for (var i = 0; i < p.Count; i++)
            {
                var dir = p[i].Groups["dirName"].Value;

                // Match file name
                var f = generator_fileName.Match(dir);
                if (f.Success)
                {
                    // Not at the last item in path : error
                    if (i != p.Count - 1)
                        throw new Exception("Format invalid");

                    FileName = f.Groups["fileName"].Value.Trim();
                    FileType = f.Groups["fileType"].Value.Trim();
                    if (string.IsNullOrEmpty(FileName))
                        throw new Exception("Format invalid");
                }
                else
                {
                    dics.Add(dir);
                }
            }

            ContainerName = parent.ContainerName;
            DirectoryName = dics.ToArray();
        }
        
        /// <summary>
        /// The container of this path.
        /// </summary>
        public string ContainerName { get; }
        
        /// <summary>
        /// The directory relation of this path.
        /// </summary>
        public string[] DirectoryName { get; }
        
        /// <summary>
        /// Is this path pointing to a file?
        /// </summary>
        public bool IsFilePath => !string.IsNullOrEmpty(FileType);
        
        /// <summary>
        /// The name of the file this path pointing to.
        /// </summary>
        public string FileName { get; }
        
        /// <summary>
        /// The type of the file this path pointing to.
        /// </summary>
        public string FileType { get; }

        private string _cachedShortPath;
        private string _cachedFullPath;

        /// <summary>
        /// Backward path from this path.
        /// </summary>
        /// <param name="distance">The distance want to back.</param>
        public FSPath NavBackward(int distance)
        {
            return new FSPath(this, distance);
        }

        /// <summary>
        /// Toward path from this path.
        /// </summary>
        /// <param name="relatePath">The relative path from this path.</param>
        public FSPath NavToward(string relatePath)
        {
            return new FSPath(this, relatePath);
        }
        
        public string ShortPath()
        {
            
            if (string.IsNullOrEmpty(_cachedShortPath))
            {
                var builder = new StringBuilder();
            
                foreach (var s in DirectoryName)
                {
                    builder.Append($"/{s}");
                }

                if (!String.IsNullOrWhiteSpace(FileType))
                {
                    builder.Append($"/{FileName}.{FileType}");
                }

                _cachedShortPath = builder.ToString();
            }
            
            return _cachedShortPath;
        }

        public string FullPath()
        {
            
            if (string.IsNullOrEmpty(_cachedFullPath))
            {
                var builder = new StringBuilder();
                builder.Append($"{ContainerName}://");
            
                foreach (var s in DirectoryName)
                {
                    builder.Append($"/{s}");
                }

                if (!String.IsNullOrWhiteSpace(FileType))
                {
                    builder.Append($"/{FileName}.{FileType}");
                }

                _cachedFullPath = builder.ToString();
            }

            return _cachedFullPath;
        }

        public override string ToString() => FullPath();

        public override int GetHashCode()
        {
            unchecked
            {
                var HashCode = ContainerName.GetHashCode();
                HashCode = (HashCode * 397) ^ DirectoryName.GetHashCode();
                HashCode = (HashCode * 397) ^ FileName.GetHashCode();
                HashCode = (HashCode * 397) ^ FileType.GetHashCode();
                return HashCode;
            }
        }

        public static bool operator ==(FSPath left, FSPath right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FSPath left, FSPath right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            if (obj is FSPath)
                return Equals((FSPath)obj);
            return false;
        }

        public bool Equals(FSPath obj)
        {
            if (obj.ContainerName == ContainerName && 
                obj.FileName == FileName && 
                obj.FileType == FileType &&
                obj.DirectoryName.Length == DirectoryName.Length)
            {
                for (int i = 0; i < DirectoryName.Length; i++)
                {
                    if (obj.DirectoryName[i] != DirectoryName[i])
                        return false;
                }

                return true;
            }

            return false;
        }
        
    }
}