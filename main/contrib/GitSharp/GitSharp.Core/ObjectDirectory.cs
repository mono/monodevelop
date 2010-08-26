/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Transport;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;

namespace GitSharp.Core
{
    /// <summary>
    /// Traditional file system based <see cref="ObjectDatabase"/>.
    /// <para />
    /// This is the classical object database representation for a Git repository,
    /// where objects are stored loose by hashing them into directories by their
    /// <see cref="ObjectId"/>, or are stored in compressed containers known as
    /// <see cref="PackFile"/>s.
    /// </summary>
    public class ObjectDirectory : ObjectDatabase
    {
        private static readonly PackList NoPacks = new PackList(-1, -1, new PackFile[0]);
        private readonly DirectoryInfo _objects;
        private readonly DirectoryInfo _infoDirectory;
        private readonly DirectoryInfo _packDirectory;
        private readonly FileInfo _alternatesFile;
        private readonly AtomicReference<PackList> _packList;

        private DirectoryInfo[] _alternateObjectDir;

        /// <summary>
        /// Initialize a reference to an on-disk object directory.
        /// </summary>
        /// <param name="dir">the location of the <code>objects</code> directory.</param>
        /// <param name="alternateObjectDir">a list of alternate object directories</param>
        public ObjectDirectory(DirectoryInfo dir, DirectoryInfo[] alternateObjectDir)
        {
            _objects = dir;
            _alternateObjectDir = alternateObjectDir;
            _infoDirectory = new DirectoryInfo(_objects.FullName + "/info");
            _packDirectory = new DirectoryInfo(_objects.FullName + "/pack");
            _alternatesFile = new FileInfo(_infoDirectory + "/alternates");
            _packList = new AtomicReference<PackList>(NoPacks);
        }

        /// <summary>
        /// Gets the location of the <code>objects</code> directory.
        /// </summary>
        public DirectoryInfo getDirectory()
        {
            return _objects;
        }

        public override bool exists()
        {
            return _objects.Exists;
        }

        public override void create()
        {
            _objects.Mkdirs();
            _infoDirectory.Mkdirs();
            _packDirectory.Mkdirs();
        }

        public override void closeSelf()
        {
            PackList packs = _packList.get();
            _packList.set(NoPacks);
            foreach (PackFile p in packs.packs)
            {
                p.Dispose();
            }

#if DEBUG
            GC.SuppressFinalize(this); // Disarm lock-release checker
#endif
        }

#if DEBUG
        // A debug mode warning if the type has not been disposed properly
        ~ObjectDirectory()
        {
            Console.Error.WriteLine(GetType().Name + " has not been properly disposed {" + _objects.FullName + "}");
        }
#endif
        /// <summary>
        /// Compute the location of a loose object file.
        /// </summary>
        /// <param name="objectId">Identity of the loose object to map to the directory.</param>
        /// <returns>Location of the object, if it were to exist as a loose object.</returns>
        public FileInfo fileFor(AnyObjectId objectId)
        {
            if (objectId == null)
                throw new ArgumentNullException("objectId");

            return fileFor(objectId.Name);
        }

        private FileInfo fileFor(string objectName)
        {
            string d = objectName.Slice(0, 2);
            string f = objectName.Substring(2);
            return new FileInfo(_objects.FullName + "/" + d + "/" + f);
        }

        /// <returns>
        /// unmodifiable collection of all known pack files local to this
        /// directory. Most recent packs are presented first. Packs most
        /// likely to contain more recent objects appear before packs
        /// containing objects referenced by commits further back in the
        /// history of the repository.
        /// </returns>
        public ICollection<PackFile> getPacks()
        {
            PackFile[] packs = _packList.get().packs;
            return new ReadOnlyCollection<PackFile>(packs);
        }

        /// <summary>
        /// Add a single existing pack to the list of available pack files.
        /// </summary>
        /// <param name="pack">Path of the pack file to open.</param>
        /// <param name="idx">Path of the corresponding index file.</param>
        ///	<exception cref="IOException">
        /// Index file could not be opened, read, or is not recognized as
        /// a Git pack file index.
        /// </exception>
        public void openPack(FileInfo pack, FileInfo idx)
        {
            if (pack == null)
                throw new ArgumentNullException("pack");
            if (idx == null)
                throw new ArgumentNullException("idx");

            string p = pack.Name;
            string i = idx.Name;

            if (p.Length != 50 || !p.StartsWith("pack-") || !p.EndsWith(IndexPack.PackSuffix))
            {
                throw new IOException("Not a valid pack " + pack);
            }

            if (i.Length != 49 || !i.StartsWith("pack-") || !i.EndsWith(IndexPack.IndexSuffix))
            {
                throw new IOException("Not a valid pack " + idx);
            }

            if (!p.Slice(0, 45).Equals(i.Slice(0, 45)))
            {
                throw new IOException("Pack " + pack + "does not match index");
            }

            InsertPack(new PackFile(idx, pack));
        }

        public override string ToString()
        {
            return "ObjectDirectory[" + getDirectory() + "]";
        }

        public override bool hasObject1(AnyObjectId objectId)
        {
            foreach (PackFile p in _packList.get().packs)
            {
                try
                {
                    if (p.HasObject(objectId))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    // The hasObject call should have only touched the index,
                    // so any failure here indicates the index is unreadable
                    // by this process, and the pack is likewise not readable.
                    //
                    RemovePack(p);
                    continue;
                }
            }

            return false;
        }

        public override ObjectLoader openObject1(WindowCursor curs, AnyObjectId objectId)
        {
            PackList pList = _packList.get();

            while (true)
            {
            SEARCH:
                foreach (PackFile p in pList.packs)
                {
                    try
                    {
                        PackedObjectLoader ldr = p.Get(curs, objectId);
                        if (ldr != null)
                        {
                            ldr.Materialize(curs);
                            return ldr;
                        }
                    }
                    catch (PackMismatchException)
                    {
                        // Pack was modified; refresh the entire pack list.
                        //
                        pList = ScanPacks(pList);
                        goto SEARCH;
                    }
                    catch (IOException)
                    {
                        // Assume the pack is corrupted.
                        //
                        RemovePack(p);
                    }
                }

                return null;
            }
        }

        public override void OpenObjectInAllPacksImplementation(ICollection<PackedObjectLoader> @out, WindowCursor windowCursor, AnyObjectId objectId)
        {
            if (@out == null)
                throw new ArgumentNullException("out");

            PackList pList = _packList.get();
            while (true)
            {
            SEARCH:
                foreach (PackFile p in pList.packs)
                {
                    try
                    {
                        PackedObjectLoader ldr = p.Get(windowCursor, objectId);
                        if (ldr != null)
                        {
                            @out.Add(ldr);
                        }
                    }
                    catch (PackMismatchException)
                    {
                        // Pack was modified; refresh the entire pack list.
                        //
                        pList = ScanPacks(pList);
                        goto SEARCH;
                    }
                    catch (IOException)
                    {
                        // Assume the pack is corrupted.
                        //
                        RemovePack(p);
                    }
                }

                break;
            }
        }

        public override bool hasObject2(string objectName)
        {
            return fileFor(objectName).Exists;
        }

        public override ObjectLoader openObject2(WindowCursor curs, string objectName, AnyObjectId objectId)
        {
            try
            {
                return new UnpackedObjectLoader(fileFor(objectName), objectId);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        public override bool tryAgain1()
        {
            PackList old = _packList.get();
            _packDirectory.Refresh();
            if (old.tryAgain(_packDirectory.lastModified()))
                return old != ScanPacks(old);

            return false;
        }

        private void InsertPack(PackFile pf)
        {
            PackList o, n;
            do
            {
                o = _packList.get();
                PackFile[] oldList = o.packs;
                var newList = new PackFile[1 + oldList.Length];
                newList[0] = pf;
                Array.Copy(oldList, 0, newList, 1, oldList.Length);
                n = new PackList(o.lastRead, o.lastModified, newList);
            } while (!_packList.compareAndSet(o, n));
        }

        private void RemovePack(PackFile deadPack)
        {
            PackList o, n;
            do
            {
                o = _packList.get();
                PackFile[] oldList = o.packs;
                int j = indexOf(oldList, deadPack);
                if (j < 0)
                    break;
                var newList = new PackFile[oldList.Length - 1];
                Array.Copy(oldList, 0, newList, 0, j);
                Array.Copy(oldList, j + 1, newList, j, newList.Length - j);
                n = new PackList(o.lastRead, o.lastModified, newList);
            } while (!_packList.compareAndSet(o, n));
            deadPack.Dispose();
        }

        private static int indexOf(PackFile[] list, PackFile pack)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == pack)
                    return i;
            }
            return -1;
        }

        private PackList ScanPacks(PackList original)
        {
            lock (_packList)
            {
                PackList o, n;
                do
                {
                    o = _packList.get();
                    if (o != original)
                    {
                        // Another thread did the scan for us, while we
                        // were blocked on the monitor above.
                        //
                        return o;
                    }
                    n = ScanPacksImpl(o);
                    if (n == o)
                        return n;
                } while (!_packList.compareAndSet(o, n));

                return n;
            }
        }

        private PackList ScanPacksImpl(PackList old)
        {
            Dictionary<string, PackFile> forReuse = ReuseMap(old);
            long lastRead = SystemReader.getInstance().getCurrentTime();
            long lastModified = _packDirectory.lastModified();
            HashSet<String> names = listPackDirectory();
            var list = new List<PackFile>(names.Count >> 2);
            bool foundNew = false;
            foreach (string indexName in names)
            {
                // Must match "pack-[0-9a-f]{40}.idx" to be an index.
                //
                if (indexName.Length != 49 || !indexName.EndsWith(".idx"))
                    continue;
                string @base = indexName.Slice(0, indexName.Length - 4);
                string packName = IndexPack.GetPackFileName(@base);

                if (!names.Contains(packName))
                {
                    // Sometimes C Git's HTTP fetch transport leaves a
                    // .idx file behind and does not download the .pack.
                    // We have to skip over such useless indexes.
                    //
                    continue;
                }
                PackFile oldPack;
                forReuse.TryGetValue(packName, out oldPack);
                forReuse.Remove(packName);
                if (oldPack != null)
                {
                    list.Add(oldPack);
                    continue;
                }

                var packFile = new FileInfo(_packDirectory.FullName + "/" + packName);

                var idxFile = new FileInfo(_packDirectory + "/" + indexName);
                list.Add(new PackFile(idxFile, packFile));
                foundNew = true;
            }

            // If we did not discover any new files, the modification time was not
            // changed, and we did not remove any files, then the set of files is
            // the same as the set we were given. Instead of building a new object
            // return the same collection.
            //
            if (!foundNew && lastModified == old.lastModified && forReuse.isEmpty())
                return old.updateLastRead(lastRead);

            foreach (PackFile p in forReuse.Values)
            {
                p.Dispose();
            }

            if (list.Count == 0)
            {
                return new PackList(lastRead, lastModified, NoPacks.packs);
            }

            PackFile[] r = list.ToArray();
            Array.Sort(r, PackFile.PackFileSortComparison);
            return new PackList(lastRead, lastModified, r);
        }

        private static Dictionary<string, PackFile> ReuseMap(PackList old)
        {
            var forReuse = new Dictionary<string, PackFile>();
            foreach (PackFile p in old.packs)
            {
                if (p.IsInvalid)
                {
                    // The pack instance is corrupted, and cannot be safely used
                    // again. Do not include it in our reuse map.
                    //
                    p.Dispose();
                    continue;
                }

                PackFile prior = forReuse[p.File.Name] = p;
                if (prior != null)
                {
                    // This should never occur. It should be impossible for us
                    // to have two pack files with the same name, as all of them
                    // came out of the same directory. If it does, we promised to
                    // close any PackFiles we did not reuse, so close the one we
                    // just evicted out of the reuse map.
                    //
                    prior.Dispose();
                }
            }

            return forReuse;
        }

        private HashSet<string> listPackDirectory()
        {
            {
                var nameList = new List<string>(_packDirectory.GetFileSystemInfos().Select(x => x.Name));
                if (nameList.Count == 0)
                    return new HashSet<string>();
                var nameSet = new HashSet<String>();
                foreach (string name in nameList)
                {
                    if (name.StartsWith("pack-"))
                        nameSet.Add(name);
                }
                return nameSet;
            }
        }

        protected override ObjectDatabase[] loadAlternates()
        {
            var l = new List<ObjectDatabase>(4);
            if (_alternateObjectDir != null)
            {
                foreach (DirectoryInfo d in _alternateObjectDir)
                {
                    l.Add(openAlternate(d));
                }
            }
            else
            {
                using (StreamReader br = Open(_alternatesFile))
                {
                    string line;
                    while ((line = br.ReadLine()) != null)
                    {
                        l.Add(openAlternate(line));
                    }
                }
            }

            if (l.isEmpty())
            {
                return NoAlternates;
            }

            return l.ToArray();
        }

        private static StreamReader Open(FileSystemInfo f)
        {
            return new StreamReader(new FileStream(f.FullName, System.IO.FileMode.Open));
        }

        private ObjectDatabase openAlternate(string location)
        {
            var objdir = (DirectoryInfo)FS.resolve(_objects, location);
            return openAlternate(objdir);
        }

        private ObjectDatabase openAlternate(DirectoryInfo objdir)
        {
            DirectoryInfo parent = objdir.Parent;
            if (RepositoryCache.FileKey.isGitRepository(parent))
            {
                Repository db = RepositoryCache.open(RepositoryCache.FileKey.exact(parent));
                return new AlternateRepositoryDatabase(db);
            }
            return new ObjectDirectory(objdir, null);
        }

        private class PackList
        {
            /// <summary>
            /// Last wall-clock time the directory was read.
            /// </summary>
            private volatile LongWrapper _lastRead = new LongWrapper();
            public long lastRead { get { return _lastRead.Value; } }

            /// <summary>
            /// Last modification time of <see cref="ObjectDirectory._packDirectory"/>.
            /// </summary>
            public readonly long lastModified;

            /// <summary>
            /// All known packs, sorted by <see cref="PackFile.PackFileSortComparison"/>.
            /// </summary>
            public readonly PackFile[] packs;

            private bool cannotBeRacilyClean;

            public PackList(long lastRead, long lastModified, PackFile[] packs)
            {
                this._lastRead.Value = lastRead;
                this.lastModified = lastModified;
                this.packs = packs;
                this.cannotBeRacilyClean = notRacyClean(lastRead);
            }

            private bool notRacyClean(long read)
            {
                return read - lastModified > 2 * 60 * 1000L;
            }

            public PackList updateLastRead(long now)
            {
                if (notRacyClean(now))
                    cannotBeRacilyClean = true;
                _lastRead.Value = now;
                return this;
            }

            public bool tryAgain(long currLastModified)
            {
                // Any difference indicates the directory was modified.
                //
                if (lastModified != currLastModified)
                    return true;

                // We have already determined the last read was far enough
                // after the last modification that any new modifications
                // are certain to change the last modified time.
                //
                if (cannotBeRacilyClean)
                    return false;

                if (notRacyClean(lastRead))
                {
                    // Our last read should have marked cannotBeRacilyClean,
                    // but this thread may not have seen the change. The read
                    // of the volatile field lastRead should have fixed that.
                    //
                    return false;
                }

                // We last read this directory too close to its last observed
                // modification time. We may have missed a modification. Scan
                // the directory again, to ensure we still see the same state.
                //
                return true;
            }

            private class LongWrapper
            {
                public LongWrapper()
                {
                    Value = -1;
                }

                public long Value { get; set; }
            }
        }

        public override ObjectDatabase newCachedDatabase()
        {
            return new CachedObjectDirectory(this);
        }
    }
}