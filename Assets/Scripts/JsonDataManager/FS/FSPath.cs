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

        private static readonly Regex
            generator_container = new Regex(@"^(?<container>[\w\d_-]+):/(?<url>[\w\d\-_/.\s]+)$"),
            generator_path = new Regex(@"\G/(?<dirName>[\w\d\-_\.\s]*)"),
            generator_fileName = new Regex(@"^(?<fileName>[\w\d\s-_\.]*)\.(?<fileType>[\w\d]*)$");
        
        public static readonly FSPath StaticContainerFSPathRoot = new FSPath("static://");
        public static readonly FSPath CurrentContainerFSPathRoot = new FSPath("current://");

        
        /// <summary>
        /// Create a new FSPath by path string (Full or short)
        /// </summary>
        /// <param name="path">Path String</param>
        public FSPath(string path)
        {
            // Safety
            path = path.Trim();
            if (String.IsNullOrWhiteSpace(path))
                throw new FormatException("Empty path can not be a valid FSPath.");

            // Init
            ContainerName = "current";
            DirectoryVector = Array.Empty<string>();
            FileIdentify = FileType = "";
            _cachedShortPath = _cachedFullPath = "";
            
            var container = generator_container.Match(path);
            MatchCollection url;
            var dics = new List<string>();
            
            // Match container name
            if (container.Success)
            {
                ContainerName = container.Groups["container"].Value.ToLower();
                // Match path
                var urlP = container.Groups["url"].Value;
                if (urlP.StartsWith("/"))
                    url = generator_path.Matches(urlP);
                else
                    throw new FormatException("You must link container and path by ://");
            }
            else
            {
                if (path == "/")
                {
                    DirectoryVector = dics.ToArray();
                    return;
                }
                // Match path
                url = generator_path.Matches(path);
                if (url.Count == 0)
                    throw new FormatException("Can not understand path.");
            }
            
            // Write url path
            for (var i = 0; i < url.Count; i++)
            {
                var dir = url[i].Groups["dirName"].Value;
                if (string.IsNullOrEmpty(dir))
                    continue;
                if (string.IsNullOrWhiteSpace(dir))
                    throw new FormatException("Directory name can not be blank!");

                // Flag of a file
                if (dir.Contains("."))
                {
                    // Not at the last item in path : error
                    if (i != url.Count - 1)
                        throw new FormatException("Directory name should not have a dot(.).");
                    
                    // Match file name
                    var f = generator_fileName.Match(dir);
                    if (f.Success)
                    {

                        FileIdentify = f.Groups["fileName"].Value;
                        FileType = f.Groups["fileType"].Value;
                        // Type is essential for a file.
                        if (string.IsNullOrEmpty(FileType))
                            throw new FormatException("File must have a type at lease.");
                        break;
                    }
                    
                    throw new FormatException("File type may include blank.");
                }
                
                dics.Add(dir);
            }
                
            DirectoryVector = dics.ToArray();
        }

        private FSPath(FSPath child, int previous)
        {
            if (child.Equals(default))
                throw new NullReferenceException(nameof(child));
            
            if (previous < 1)
                throw new ArgumentOutOfRangeException(nameof(previous));

            if (!string.IsNullOrEmpty(child.FileType))
                previous -= 1;

            FileIdentify = FileType = "";
            _cachedShortPath = _cachedFullPath = "";
            ContainerName = child.ContainerName;
            
            List<string> ary = new List<string>();
            for (var i = 0; i < child.DirectoryVector.Length - previous; i++)
            {
                ary.Add(child.DirectoryVector[i]);
            }

            DirectoryVector = ary.ToArray();
        }

        private FSPath(FSPath parent, string location)
        {
            
            if (parent.Equals(default))
                throw new ArgumentNullException(nameof(parent));

            if (string.IsNullOrEmpty(location))
                throw new ArgumentNullException(nameof(location));

            if (string.IsNullOrEmpty(parent.FileType))
                throw new InvalidOperationException("Parent should be a directory, not a file!");

            _cachedShortPath = _cachedFullPath = "";
            FileIdentify = FileType = "";

            var url = generator_path.Matches($"/{location}");
            if (url.Count == 0)
                throw new Exception("Invalid Format");
                    
            var dics = new List<string>(parent.DirectoryVector);
            for (var i = 0; i < url.Count; i++)
            {
                var dir = url[i].Groups["dirName"].Value;
                if (string.IsNullOrEmpty(dir))
                    continue;
                if (string.IsNullOrWhiteSpace(dir))
                    throw new FormatException("Directory name can not be blank!");

                // Flag of a file
                if (dir.Contains("."))
                {
                    // Not at the last item in path : error
                    if (i != url.Count - 1)
                        throw new FormatException("Directory name should not have a dot(.).");
                    
                    // Match file name
                    var f = generator_fileName.Match(dir);
                    if (f.Success)
                    {

                        FileIdentify = f.Groups["fileName"].Value;
                        FileType = f.Groups["fileType"].Value;
                        // Type is essential for a file.
                        if (string.IsNullOrEmpty(FileType))
                            throw new FormatException("File must have a type at lease.");
                        break;
                    }
                    
                    throw new FormatException("File type may include blank.");
                }
                
                dics.Add(dir);
            }

            ContainerName = parent.ContainerName;
            DirectoryVector = dics.ToArray();
        }
        
        /// <summary>
        /// The container of this path.
        /// </summary>
        public string ContainerName { get; private set; }
        
        /// <summary>
        /// The directory relation of this path.
        /// </summary>
        public string[] DirectoryVector { get; }

        /// <summary>
        /// The directory name of this path
        /// </summary>
        public string DirectoryName
        {
            get
            {
                if (DirectoryVector.Length == 0)
                    return ContainerName;

                return DirectoryVector[DirectoryVector.Length - 1];
            }
        }

        /// <summary>
        /// Is this path's container is "current".
        /// </summary>
        public bool IsFloating => ContainerName == "current";
        
        /// <summary>
        /// Is this path pointing to a file?
        /// </summary>
        public bool IsFilePath => !string.IsNullOrEmpty(FileType);

        /// <summary>
        /// The name of the file this path pointing to.
        /// </summary>
        public string FileName => $"{FileIdentify}.{FileType}";
        
        /// <summary>
        /// The identify of the file this path pointing to.
        /// </summary>
        public string FileIdentify { get; }
        
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
        public readonly FSPath NavBackward(int distance)
        {
            return new FSPath(this, distance);
        }

        /// <summary>
        /// Toward path from this path.
        /// </summary>
        /// <param name="relatePath">The relative path from this path.</param>
        public readonly FSPath NavToward(string relatePath)
        {
            if (IsFilePath)
                throw new InvalidOperationException("File can not contain a directory or file!");
            return new FSPath(this, relatePath);
        }

        /// <summary>
        /// Using a correct container to clean floating status
        /// </summary>
        /// <param name="container">The new container name.</param>
        public FSPath CleanFloating(string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentNullException(nameof(container));

            if (container == "current")
                throw new ArgumentException(nameof(container));
            
            if (ContainerName == "current")
                ContainerName = container;

            return this;
        }
        
        public string ShortPath()
        {
            
            if (string.IsNullOrEmpty(_cachedShortPath))
            {
                var builder = new StringBuilder();
            
                bool w = false;
                foreach (var s in DirectoryVector)
                {
                    w = true;
                    builder.Append($"/{s}");
                }

                if (!String.IsNullOrWhiteSpace(FileType))
                {
                    w = true;
                    builder.Append($"/{FileIdentify}.{FileType}");
                }

                if (!w)
                    builder.Append("/");
                
                _cachedShortPath = builder.ToString();
            }
            
            return _cachedShortPath;
        }

        public string FullPath()
        {
            
            if (string.IsNullOrEmpty(_cachedFullPath))
            {
                var builder = new StringBuilder();
                builder.Append($"{ContainerName}:/");

                bool w = false;
                foreach (var s in DirectoryVector)
                {
                    w = true;
                    builder.Append($"/{s}");
                }

                if (!String.IsNullOrWhiteSpace(FileType))
                {
                    w = true;
                    builder.Append($"/{FileIdentify}.{FileType}");
                }

                if (!w)
                    builder.Append("/");

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
                HashCode = (HashCode * 397) ^ DirectoryVector.GetHashCode();
                HashCode = (HashCode * 397) ^ FileIdentify.GetHashCode();
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
                obj.FileIdentify == FileIdentify && 
                obj.FileType == FileType &&
                obj.DirectoryVector.Length == DirectoryVector.Length)
            {
                for (int i = 0; i < DirectoryVector.Length; i++)
                {
                    if (obj.DirectoryVector[i] != DirectoryVector[i])
                        return false;
                }

                return true;
            }

            return false;
        }
        
    }
}