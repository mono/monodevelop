using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Xunit;
using Microsoft.WindowsAPICodePack.Shell;
using System.Threading;
using System.Diagnostics;

namespace Tests
{
    public class ShellObjectWatcherTests
    {
        private static readonly int TimeoutMS = 5000;

        [Fact]
        public void ItemRenamedTest()
        {
            TestMethod("ItemRenamed", x => File.Move(x, x + "renamed"), false);
        }

        [Fact]
        public void ItemCreatedTest()
        {
            TestMethod("ItemCreated", x => File.WriteAllText(Path.Combine(x, "tempFile"), "This file is created"), true);
        }

        [Fact]
        public void ItemDeletedTest()
        {
            TestMethod("ItemDeleted", x => File.Delete(x), false);
        }

        [Fact]
        public void UpdatedTest()
        {
            TestMethod("Updated", x => File.WriteAllText(Path.Combine(x, "tempFile"), "contents changed or created"), true);
        }

        [Fact]
        public void DirectoryUpdatedTest()
        {
            TestMethod("DirectoryUpdated", x => File.WriteAllText(Path.Combine(x, "tempFile"), "Contents updated"), true);
        }

        [Fact]
        public void DirectoryRenamedTest()
        {
            TestMethod("DirectoryRenamed", x => Directory.Move(x, x + "renamed"), true);
        }

        [Fact]
        public void DirectoryCreatedTest()
        {
            TestMethod("DirectoryCreated", x => Directory.CreateDirectory(Path.Combine(x, "NewDir")), true);
        }

        [Fact]
        public void DirectoryDeletedTest()
        {
            TestMethod("DirectoryDeleted", x => Directory.Delete(Directory.CreateDirectory(Path.Combine(x, "NewDir")).FullName), true);
        }

        [Fact(Skip = "How?")]
        public void MediaInsertedTest() { }
        [Fact(Skip = "How?")]
        public void MediaRemovedTest() { }
        [Fact(Skip = "How?")]
        public void DriveAddedTest() { }
        [Fact(Skip = "How?")]
        public void DriveRemovedTest() { }
        [Fact(Skip = "How?")]
        public void FolderNetworkSharedTest() { }
        [Fact(Skip = "How?")]
        public void FolderNetworkUnsharedTest() { }
        [Fact(Skip = "How?")]
        public void ServerDisconnectedTest() { }

        [Fact]
        public void FreeSpaceChangedTest()
        {
            TestMethod("FreeSpaceChanged", x =>
            {
                byte[] data = new byte[2048];
                new Random().NextBytes(data);
                File.WriteAllBytes(Path.GetTempFileName(), data);
            }, @"C:\");
        }

        [Fact(Skip = "How?")]
        public void FileTypeAssociationChanged() { }

        private void TestMethod(string eventName, Action<string> test, bool folder)
        {
            string path = folder ? CreateTempFolder() : CreateTempFile();
            TestMethod(eventName, test, path);
        }
        private void TestMethod(string eventName, Action<string> test, string path)
        {
            var shellObject = ShellObject.FromParsingName(path);

            using (AutoResetEvent evt = new AutoResetEvent(false))
            using (ShellObjectWatcher watcher = new ShellObjectWatcher(shellObject, true))
            {
                bool success = false;

                Action successEvent = new Action(() =>
                {
                    success = true;
                    evt.Set();
                });

                var changedHandler = new EventHandler<ShellObjectChangedEventArgs>((sender, args) => successEvent());
                var renamedHandler = new EventHandler<ShellObjectRenamedEventArgs>((sender, args) => successEvent());

                //register for event
                var addMethod = typeof(ShellObjectWatcher)
                .GetEvents()
                .FirstOrDefault(x => x.Name.Equals(eventName, StringComparison.InvariantCultureIgnoreCase))
                .GetAddMethod();

                var paramType = addMethod.GetParameters()[0].ParameterType
                    .GetGenericArguments()[0];
                if (paramType == typeof(ShellObjectChangedEventArgs))
                {
                    addMethod.Invoke(watcher, new object[] { changedHandler });
                }
                else if (paramType == typeof(ShellObjectRenamedEventArgs))
                {
                    addMethod.Invoke(watcher, new object[] { renamedHandler });
                }
                else
                {
                    throw new Exception("Unknown handler type.");
                }

                //start
                watcher.Start();

                test(path);

                evt.WaitOne(TimeoutMS);

                Assert.True(success);
            }
        }

        private static string CreateTempFile()
        {
            //string path = string.Format("text{0}.txt", DateTime.Now.Millisecond);
            //File.WriteAllText(path, string.Empty);
            //return Path.GetFullPath(path);
            var test = Path.GetTempFileName();
            File.AppendAllText(test, "no longer an empty file");
            return test;
        }

        private static string CreateTempFolder()
        {
            string path = Path.Combine(Path.GetTempPath(), "temp" + DateTime.Now.Millisecond);
            return Directory.CreateDirectory(path).FullName;
        }
    }
}
