using System.Collections.Generic;
using System.IO;

namespace YetAnotherRelogger.Helpers.Tools
{
    public class FileListCache
    {
        public FileListCache(string path)
        {
            _rootpath = path;
            updatelist(path);
        }

        public HashSet<MyFile> FileList;
        private readonly string _rootpath;

        private void updatelist(string path, bool newlist = true)
        {
            if (newlist) FileList = new HashSet<MyFile>();
            if (!path.Equals(_rootpath))
            {
                FileList.Add(new MyFile
                {
                    Path = path.Substring(_rootpath.Length + 1),
                    directory = true
                });
            }
            foreach (var file in Directory.GetFiles(path))
            {
                FileList.Add(new MyFile
                {
                    Path = file.Substring(_rootpath.Length + 1),
                    directory = false
                });
            }

            foreach (var dir in Directory.GetDirectories(path))
                updatelist(dir, false);
        }

        public struct MyFile
        {
            public string Path;
            public bool directory;
        }
    }
}
