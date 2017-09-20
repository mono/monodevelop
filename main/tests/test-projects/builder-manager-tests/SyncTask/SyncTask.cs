using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SyncTask
{
    public class FileSyncTask: Task
    {
        [Required]
        public string Path { get; set; }

        public override bool Execute()
        {
            var syncFile = System.IO.Path.Combine(Path, "sync-event");
            var continueFile = System.IO.Path.Combine(Path, "continue-event");

            File.WriteAllText(syncFile, "");

            while (!File.Exists(continueFile))
                System.Threading.Thread.Sleep(50);
            return true;
        }
    }
}
