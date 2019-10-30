using System.IO;
using System.Collections.Generic;
using System;

namespace Kayac
{
    public class FileWatcher : IDisposable
    {
        public FileWatcher(string path)
        {
#if UNITY_EDITOR || UNITY_STANDALONE // 念のため封じておく
            watcher = new FileSystemWatcher();
            watcher.Path = Path.GetFullPath(path);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.IncludeSubdirectories = true;
            watcher.Changed += OnChanged;
            watcher.EnableRaisingEvents = true;
#endif
            changedPaths = new HashSet<string>();
        }

        public void Dispose()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            watcher.Dispose();
#endif
            watcher = null;
        }

        public bool IsChanged()
        {
            return (changedPaths.Count > 0);
        }

        public string GetChangedPath()
        {
            string ret = null;
            if (changedPaths.Count > 0)
            {
                foreach (var item in changedPaths) // 一個取り出し
                {
                    ret = item;
                    break;
                }
                changedPaths.Remove(ret);
            }
            return ret;
        }

        // 別スレで呼ばれることに注意
        void OnChanged(object sender, FileSystemEventArgs args)
        {
            // まだ取得されてないパスのみ記録
            if (!changedPaths.Contains(args.FullPath))
            {
                changedPaths.Add(args.FullPath);
            }
        }

        // non-public ----------
        FileSystemWatcher watcher;
        HashSet<string> changedPaths;
    }
}

