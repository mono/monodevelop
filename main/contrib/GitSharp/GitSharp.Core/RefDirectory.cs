/*
 * Copyright (C) 2010, Google Inc.
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;
using File = System.IO.File;

namespace GitSharp.Core
{
    /// <summary>
    /// Traditional file system based {@link RefDatabase}.
    /// <para/>
    /// This is the classical reference database representation for a Git repository.
    /// References are stored in two formats: loose, and packed.
    /// <para/>
    /// Loose references are stored as individual files within the {@code refs/}
    /// directory. The file name matches the reference name and the file contents is
    /// the current {@link ObjectId} in string form.
    /// <para/>
    /// Packed references are stored in a single text file named {@code packed-refs}.
    /// In the packed format, each reference is stored on its own line. This file
    /// reduces the number of files needed for large reference spaces, reducing the
    /// overall size of a Git repository on disk.
    /// </summary>
    public class RefDirectory : RefDatabase
    {
        /// <summary>
        /// Magic string denoting the start of a symbolic reference file.
        /// </summary>
        public static string SYMREF = "ref: "; //$NON-NLS-1$

        /// <summary>
        /// Magic string denoting the header of a packed-refs file.
        /// </summary>
        public static string PACKED_REFS_HEADER = "# pack-refs with:"; //$NON-NLS-1$

        /// <summary>
        /// If in the header, denotes the file has peeled data.
        /// </summary>
        public static string PACKED_REFS_PEELED = " peeled"; //$NON-NLS-1$

        private readonly Repository parent;

        private readonly DirectoryInfo gitDir;

        private readonly DirectoryInfo refsDir;

        private readonly DirectoryInfo logsDir;

        private readonly DirectoryInfo logsRefsDir;

        private readonly FileInfo packedRefsFile;

        /// <summary>
        /// Immutable sorted list of loose references.
        /// <para/>
        /// Symbolic references in this collection are stored unresolved, that is
        /// their target appears to be a new reference with no ObjectId. These are
        /// converted into resolved references during a get operation, ensuring the
        /// live value is always returned.
        /// </summary>
        private readonly AtomicReference<RefList<LooseRef>> looseRefs = new AtomicReference<RefList<LooseRef>>();

        /// <summary>
        /// Immutable sorted list of packed references.
        /// </summary>
        private readonly AtomicReference<PackedRefList> packedRefs = new AtomicReference<PackedRefList>();

        /// <summary>
        /// Number of modifications made to this database.
        /// <para/>
        /// This counter is incremented when a change is made, or detected from the
        /// filesystem during a read operation.
        /// </summary>
        private readonly AtomicInteger modCnt = new AtomicInteger();

        /// <summary>
        /// Last <see cref="modCnt"/> that we sent to listeners.
        /// <para/>
        /// This value is compared to <see cref="modCnt"/>, and a notification is sent to
        /// the listeners only when it differs.
        /// </summary>
        private readonly AtomicInteger lastNotifiedModCnt = new AtomicInteger();

        public RefDirectory(Repository db)
        {
            parent = db;
            gitDir = db.Directory;
            refsDir = PathUtil.CombineDirectoryPath(gitDir, Constants.R_REFS);
            logsDir = PathUtil.CombineDirectoryPath(gitDir, Constants.LOGS);
            logsRefsDir = PathUtil.CombineDirectoryPath(gitDir, Constants.LOGS + Path.DirectorySeparatorChar + Constants.R_REFS);
            packedRefsFile = PathUtil.CombineFilePath(gitDir, Constants.PACKED_REFS);

            looseRefs.set(RefList<LooseRef>.emptyList());
            packedRefs.set(PackedRefList.NO_PACKED_REFS);
        }

        public Repository getRepository()
        {
            return parent;
        }

        public override void create()
        {
            refsDir.Mkdirs();
            logsDir.Mkdirs();
            logsRefsDir.Mkdirs();

            PathUtil.CombineDirectoryPath(refsDir, Constants.R_HEADS.Substring(Constants.R_REFS.Length)).Mkdirs();
            PathUtil.CombineDirectoryPath(refsDir, Constants.R_TAGS.Substring(Constants.R_REFS.Length)).Mkdirs();
            PathUtil.CombineDirectoryPath(logsRefsDir, Constants.R_HEADS.Substring(Constants.R_REFS.Length)).Mkdirs();
        }


        public override void close()
        {
            // We have no resources to close.
        }

        public void rescan()
        {
            looseRefs.set(RefList<LooseRef>.emptyList());
            packedRefs.set(PackedRefList.NO_PACKED_REFS);
        }


        public override bool isNameConflicting(string name)
        {
            RefList<Ref> packed = getPackedRefs();
            RefList<LooseRef> loose = getLooseRefs();

            // Cannot be nested within an existing reference.
            int lastSlash = name.LastIndexOf('/');
            while (0 < lastSlash)
            {
                string needle = name.Slice(0, lastSlash);
                if (loose.contains(needle) || packed.contains(needle))
                    return true;
                lastSlash = name.LastIndexOf('/', lastSlash - 1);
            }

            // Cannot be the container of an existing reference.
            string prefix = name + '/';
            int idx;

            idx = -(packed.find(prefix) + 1);
            if (idx < packed.size() && packed.get(idx).getName().StartsWith(prefix))
                return true;

            idx = -(loose.find(prefix) + 1);
            if (idx < loose.size() && loose.get(idx).getName().StartsWith(prefix))
                return true;

            return false;
        }

        private RefList<LooseRef> getLooseRefs()
        {
            RefList<LooseRef> oldLoose = looseRefs.get();

            var scan = new LooseScanner(oldLoose, this);
            scan.scan(ALL);

            RefList<LooseRef> loose;
            if (scan.newLoose != null)
            {
                loose = scan.newLoose.toRefList();
                if (looseRefs.compareAndSet(oldLoose, loose))
                    modCnt.incrementAndGet();
            }
            else
                loose = oldLoose;
            return loose;
        }


        public override Ref getRef(string needle)
        {
            RefList<Ref> packed = getPackedRefs();
            Ref @ref = null;
            foreach (string prefix in SEARCH_PATH)
            {
                @ref = readRef(prefix + needle, packed);
                if (@ref != null)
                {
                    @ref = resolve(@ref, 0, null, null, packed);
                    break;
                }
            }
            fireRefsChanged();
            return @ref;
        }


        public override IDictionary<string, Ref> getRefs(string prefix)
        {
            RefList<Ref> packed = getPackedRefs();
            RefList<LooseRef> oldLoose = looseRefs.get();

            var scan = new LooseScanner(oldLoose, this);
            scan.scan(prefix);

            RefList<LooseRef> loose;
            if (scan.newLoose != null)
            {
                loose = scan.newLoose.toRefList();
                if (looseRefs.compareAndSet(oldLoose, loose))
                    modCnt.incrementAndGet();
            }
            else
                loose = oldLoose;
            fireRefsChanged();

            RefList<Ref>.Builder<Ref> symbolic = scan.symbolic;
            for (int idx = 0; idx < symbolic.size(); )
            {
                Ref @ref = symbolic.get(idx);
                @ref = resolve(@ref, 0, prefix, loose, packed);
                if (@ref != null && @ref.getObjectId() != null)
                {
                    symbolic.set(idx, @ref);
                    idx++;
                }
                else
                {
                    // A broken symbolic reference, we have to drop it from the
                    // collections the client is about to receive. Should be a
                    // rare occurrence so pay a copy penalty.
                    loose = loose.remove(idx);
                    symbolic.remove(idx);
                }
            }

            return new RefMap(prefix, packed, upcast(loose), symbolic.toRefList());
        }

        private RefList<Ref> upcast<TRef>(RefList<TRef> loose) where TRef : Ref
        {
            var list = new List<Ref>(loose.asList());
            return new RefList<Ref>(list.ToArray(), list.Count);
        }

        private class LooseScanner
        {
            private readonly RefDirectory _refDirectory;
            private readonly RefList<LooseRef> curLoose;

            private int curIdx;

            public readonly RefList<Ref>.Builder<Ref> symbolic = new RefList<Ref>.Builder<Ref>(4);

            public RefList<LooseRef>.Builder<LooseRef> newLoose;

            public LooseScanner(RefList<LooseRef> curLoose, RefDirectory refDirectory)
            {
                this.curLoose = curLoose;
                _refDirectory = refDirectory;
            }

            public void scan(string prefix)
            {
                if (ALL.Equals(prefix))
                {
                    scanOne(Constants.HEAD);
                    scanTree(Constants.R_REFS, _refDirectory.refsDir);

                    // If any entries remain, they are deleted, drop them.
                    if (newLoose == null && curIdx < curLoose.size())
                        newLoose = curLoose.copy(curIdx);

                }
                else if (prefix.StartsWith(Constants.R_REFS) && prefix.EndsWith("/"))
                {
                    curIdx = -(curLoose.find(prefix) + 1);
                    DirectoryInfo dir = PathUtil.CombineDirectoryPath(_refDirectory.refsDir, prefix.Substring(Constants.R_REFS.Length));
                    scanTree(prefix, dir);

                    // Skip over entries still within the prefix; these have
                    // been removed from the directory.
                    while (curIdx < curLoose.size())
                    {
                        if (!curLoose.get(curIdx).getName().StartsWith(prefix))
                            break;
                        if (newLoose == null)
                            newLoose = curLoose.copy(curIdx);
                        curIdx++;
                    }

                    // Keep any entries outside of the prefix space, we
                    // do not know anything about their status.
                    if (newLoose != null)
                    {
                        while (curIdx < curLoose.size())
                            newLoose.add(curLoose.get(curIdx++));
                    }
                }
            }

            private void scanTree(string prefix, DirectoryInfo dir)
            {
                FileSystemInfo[] entries = dir.GetFileSystemInfos();
                var entries2 = entries.Where(fsi => LockFile.FILTER(fsi.Name));
                var entries3 = entries2.ToList();

                entries = entries3.ToArray();

                if (entries != null && 0 < entries.Length)
                {
                    //                    Array.Sort(entries);
                    foreach (FileSystemInfo e in entries)
                    {
                        if (e.IsDirectory())
                            scanTree(prefix + e.Name + '/', (DirectoryInfo)e);
                        else
                            scanOne(prefix + e.Name);
                    }
                }
            }

            private void scanOne(string name)
            {
                LooseRef cur;

                if (curIdx < curLoose.size())
                {
                    do
                    {
                        cur = curLoose.get(curIdx);
                        int cmp = RefComparator.compareTo(cur, name);
                        if (cmp < 0)
                        {
                            // Reference is not loose anymore, its been deleted.
                            // Skip the name in the new result list.
                            if (newLoose == null)
                                newLoose = curLoose.copy(curIdx);
                            curIdx++;
                            cur = null;
                            continue;
                        }

                        if (cmp > 0) // Newly discovered loose reference.
                            cur = null;
                        break;
                    } while (curIdx < curLoose.size());
                }
                else
                    cur = null; // Newly discovered loose reference.

                LooseRef n;
                try
                {
                    n = _refDirectory.scanRef(cur, name);
                }
                catch (IOException)
                {
                    n = null;
                }

                if (n != null)
                {
                    if (cur != n && newLoose == null)
                        newLoose = curLoose.copy(curIdx);
                    if (newLoose != null)
                        newLoose.add(n);
                    if (n.isSymbolic())
                        symbolic.add(n);
                }
                else if (cur != null)
                {
                    // Tragically, this file is no longer a loose reference.
                    // Kill our cached entry of it.
                    if (newLoose == null)
                        newLoose = curLoose.copy(curIdx);
                }

                if (cur != null)
                    curIdx++;
            }
        }

        public override Ref peel(Ref @ref)
        {
            Ref leaf = @ref.getLeaf();
            if (leaf.isPeeled() || leaf.getObjectId() == null)
                return @ref;

            RevWalk.RevWalk rw = new RevWalk.RevWalk(getRepository());
            RevObject obj = rw.parseAny(leaf.getObjectId());
            ObjectIdRef newLeaf;
            if (obj is RevTag)
            {
                do
                {
                    obj = rw.parseAny(((RevTag)obj).getObject());
                } while (obj is RevTag);

                newLeaf = new PeeledTag(leaf.getStorage(), leaf
                                                               .getName(), leaf.getObjectId(), obj.Copy());
            }
            else
            {
                newLeaf = new PeeledNonTag(leaf.getStorage(), leaf
                                                                  .getName(), leaf.getObjectId());
            }

            // Try to remember this peeling in the cache, so we don't have to do
            // it again in the future, but only if the reference is unchanged.
            if (leaf.getStorage().IsLoose)
            {
                RefList<LooseRef> curList = looseRefs.get();
                int idx = curList.find(leaf.getName());
                if (0 <= idx && curList.get(idx) == leaf)
                {
                    LooseRef asPeeled = ((LooseRef)leaf).peel(newLeaf);
                    RefList<LooseRef> newList = curList.set(idx, asPeeled);
                    looseRefs.compareAndSet(curList, newList);
                }
            }

            return recreate(@ref, newLeaf);
        }

        private static Ref recreate(Ref old, ObjectIdRef leaf)
        {
            if (old.isSymbolic())
            {
                Ref dst = recreate(old.getTarget(), leaf);
                return new SymbolicRef(old.getName(), dst);
            }
            return leaf;
        }

        public void storedSymbolicRef(RefDirectoryUpdate u, long modified, string target)
        {
            putLooseRef(newSymbolicRef(modified, u.getRef().getName(), target));
            fireRefsChanged();
        }

        public override RefUpdate newUpdate(string name, bool detach)
        {
            RefList<Ref> packed = getPackedRefs();
            Ref @ref = readRef(name, packed);
            if (@ref != null)
                @ref = resolve(@ref, 0, null, null, packed);
            if (@ref == null)
                @ref = new Unpeeled(Storage.New, name, null);
            else if (detach && @ref.isSymbolic())
                @ref = new Unpeeled(Storage.Loose, name, @ref.getObjectId());
            return new RefDirectoryUpdate(this, @ref);
        }

        public override RefRename newRename(string fromName, string toName)
        {
            RefUpdate from = newUpdate(fromName, false);
            RefUpdate to = newUpdate(toName, false);
            return new RefDirectoryRename((RefDirectoryUpdate)from, (RefDirectoryUpdate)to);
        }

        public void stored(RefDirectoryUpdate update, long modified)
        {
            ObjectId target = update.getNewObjectId().Copy();
            Ref leaf = update.getRef().getLeaf();
            putLooseRef(new LooseUnpeeled(modified, leaf.getName(), target));
        }

        private void putLooseRef(LooseRef @ref)
        {
            RefList<LooseRef> cList, nList;
            do
            {
                cList = looseRefs.get();
                nList = cList.put(@ref);
            } while (!looseRefs.compareAndSet(cList, nList));
            modCnt.incrementAndGet();
            fireRefsChanged();
        }

        public void delete(RefDirectoryUpdate update)
        {
            Ref dst = update.getRef().getLeaf();
            string name = dst.getName();

            // Write the packed-refs file using an atomic update. We might
            // wind up reading it twice, before and after the lock, to ensure
            // we don't miss an edit made externally.
            PackedRefList packed = getPackedRefs();
            if (packed.contains(name))
            {
                var lck = new LockFile(packedRefsFile);
                if (!lck.Lock())
                    throw new IOException("Cannot lock " + packedRefsFile);
                try
                {
                    PackedRefList cur = readPackedRefs(0, 0);
                    int idx = cur.find(name);
                    if (0 <= idx)
                        commitPackedRefs(lck, cur.remove(idx), packed);
                }
                finally
                {
                    lck.Unlock();
                }
            }

            RefList<LooseRef> curLoose, newLoose;
            do
            {
                curLoose = looseRefs.get();
                int idx = curLoose.find(name);
                if (idx < 0)
                    break;
                newLoose = curLoose.remove(idx);
            } while (!looseRefs.compareAndSet(curLoose, newLoose));

            int levels = levelsIn(name) - 2;
            delete(logFor(name), levels);
            if (dst.getStorage().IsLoose)
            {
                update.unlock();
                delete(fileFor(name), levels);
            }

            modCnt.incrementAndGet();
            fireRefsChanged();
        }

        public void log(RefUpdate update, string msg, bool deref)
        {
            ObjectId oldId = update.getOldObjectId();
            ObjectId newId = update.getNewObjectId();
            Ref @ref = update.getRef();

            PersonIdent ident = update.getRefLogIdent();
            if (ident == null)
                ident = new PersonIdent(parent);
            else
                ident = new PersonIdent(ident);

            var r = new StringBuilder();
            r.Append(ObjectId.ToString(oldId));
            r.Append(' ');
            r.Append(ObjectId.ToString(newId));
            r.Append(' ');
            r.Append(ident.ToExternalString());
            r.Append('\t');
            r.Append(msg);
            r.Append('\n');
            byte[] rec = Constants.encode(r.ToString());

            if (deref && @ref.isSymbolic())
            {
                log(@ref.getName(), rec);
                log(@ref.getLeaf().getName(), rec);
            }
            else
            {
                log(@ref.getName(), rec);
            }
        }

        private void log(string refName, byte[] rec)
        {
            FileInfo log = logFor(refName);
            bool write;
            if (isLogAllRefUpdates() && shouldAutoCreateLog(refName))
                write = true;
            else if (log.IsFile())
                write = true;
            else
                write = false;

            if (write)
            {
                DirectoryInfo dir = log.Directory;
                if (!dir.Mkdirs() && !dir.IsDirectory())
                {
                    throw new IOException("Cannot create directory " + dir);
                }

                using (var sw = File.Open(log.FullName, System.IO.FileMode.Append))
                using (var @out = new BinaryWriter(sw))
                {
                    @out.Write(rec);
                }
            }
        }

        private bool isLogAllRefUpdates()
        {
            return parent.Config.getCore().isLogAllRefUpdates();
        }

        private bool shouldAutoCreateLog(string refName)
        {
            return refName.Equals(Constants.HEAD) //
                   || refName.StartsWith(Constants.R_HEADS) //
                   || refName.StartsWith(Constants.R_REMOTES);
        }

        private Ref resolve(Ref @ref, int depth, string prefix,
                            RefList<LooseRef> loose, RefList<Ref> packed)
        {
            if (@ref.isSymbolic())
            {
                Ref dst = @ref.getTarget();

                if (MAX_SYMBOLIC_REF_DEPTH <= depth)
                    return null; // claim it doesn't exist

                // If the cached value can be assumed to be current due to a
                // recent scan of the loose directory, use it.
                if (loose != null && dst.getName().StartsWith(prefix))
                {
                    int idx;
                    if (0 <= (idx = loose.find(dst.getName())))
                        dst = loose.get(idx);
                    else if (0 <= (idx = packed.find(dst.getName())))
                        dst = packed.get(idx);
                    else
                        return @ref;
                }
                else
                {
                    dst = readRef(dst.getName(), packed);
                    if (dst == null)
                        return @ref;
                }

                dst = resolve(dst, depth + 1, prefix, loose, packed);
                if (dst == null)
                    return null;
                return new SymbolicRef(@ref.getName(), dst);
            }
            return @ref;
        }

        private PackedRefList getPackedRefs()
        {
            long size = 0;
            if (File.Exists(packedRefsFile.FullName))
            {
                size = packedRefsFile.Length;
            }

            long mtime = size != 0 ? packedRefsFile.lastModified() : 0;

            PackedRefList curList = packedRefs.get();
            if (size == curList.lastSize && mtime == curList.lastModified)
                return curList;

            PackedRefList newList = readPackedRefs(size, mtime);
            if (packedRefs.compareAndSet(curList, newList))
                modCnt.incrementAndGet();
            return newList;
        }

        private PackedRefList readPackedRefs(long size, long mtime)
        {
            if (!File.Exists(packedRefsFile.FullName))
            {
                // Ignore it and leave the new list empty.
                return PackedRefList.NO_PACKED_REFS;
            }

            using (var br = new StreamReader(packedRefsFile.FullName, Constants.CHARSET))
            {
                return new PackedRefList(parsePackedRefs(br), size, mtime);
            }
        }

        private static RefList<Ref> parsePackedRefs(TextReader br)
        {
            var all = new RefList<Ref>.Builder<Ref>();
            Ref last = null;
            bool peeled = false;
            bool needSort = false;

            string p;
            while ((p = br.ReadLine()) != null)
            {
                if (p[0] == '#')
                {
                    if (p.StartsWith(PACKED_REFS_HEADER))
                    {
                        p = p.Substring(PACKED_REFS_HEADER.Length);
                        peeled = p.Contains(PACKED_REFS_PEELED);
                    }
                    continue;
                }

                if (p[0] == '^')
                {
                    if (last == null)
                        throw new IOException("Peeled line before ref.");

                    ObjectId id = ObjectId.FromString(p.Substring(1));
                    last = new PeeledTag(Storage.Packed, last.getName(), last
                                                                             .getObjectId(), id);
                    all.set(all.size() - 1, last);
                    continue;
                }

                int sp = p.IndexOf(' ');
                ObjectId id2 = ObjectId.FromString(p.Slice(0, sp));
                string name = copy(p, sp + 1, p.Length);
                ObjectIdRef cur;
                if (peeled)
                    cur = new PeeledNonTag(Storage.Packed, name, id2);
                else
                    cur = new Unpeeled(Storage.Packed, name, id2);
                if (last != null && RefComparator.compareTo(last, cur) > 0)
                    needSort = true;
                all.add(cur);
                last = cur;
            }

            if (needSort)
                all.sort();
            return all.toRefList();
        }

        private static string copy(string src, int off, int end)
        {
            // Don't use substring since it could leave a reference to the much
            // larger existing string. Force construction of a full new object.
            return new StringBuilder(end - off).Append(src, off, end - off).ToString();
        }

        private void commitPackedRefs(LockFile lck, RefList<Ref> refs, PackedRefList oldPackedList)
        {
            new PackedRefsWriter(lck, refs, oldPackedList, packedRefs).writePackedRefs();
        }

        private class PackedRefsWriter : RefWriter
        {
            private readonly LockFile _lck;
            private readonly RefList<Ref> _refs;
            private readonly PackedRefList _oldPackedList;
            private readonly AtomicReference<PackedRefList> _packedRefs;

            public PackedRefsWriter(LockFile lck, RefList<Ref> refs, PackedRefList oldPackedList, AtomicReference<PackedRefList> packedRefs)
                : base(refs)
            {
                _lck = lck;
                _refs = refs;
                _oldPackedList = oldPackedList;
                _packedRefs = packedRefs;
            }

            protected override void writeFile(string name, byte[] content)
            {
                _lck.setNeedStatInformation(true);
                try
                {
                    _lck.Write(content);
                }
                catch (IOException ioe)
                {
                    throw new ObjectWritingException("Unable to write " + name,
                                                     ioe);
                }
                try
                {
                    _lck.waitForStatChange();
                }
                catch (ThreadAbortException)
                {
                    _lck.Unlock();
                    throw new ObjectWritingException("Interrupted writing "
                                                     + name);
                }
                if (!_lck.Commit())
                    throw new ObjectWritingException("Unable to write " + name);

                _packedRefs.compareAndSet(_oldPackedList, new PackedRefList(_refs,
                                                                            content.Length, _lck.CommitLastModified));
            }
        }
        private Ref readRef(string name, RefList<Ref> packed)
        {
            RefList<LooseRef> curList = looseRefs.get();
            int idx = curList.find(name);
            if (0 <= idx)
            {
                LooseRef o = curList.get(idx);
                LooseRef n = scanRef(o, name);
                if (n == null)
                {
                    if (looseRefs.compareAndSet(curList, curList.remove(idx)))
                        modCnt.incrementAndGet();
                    return packed.get(name);
                }

                if (o == n)
                    return n;
                if (looseRefs.compareAndSet(curList, curList.set(idx, n)))
                    modCnt.incrementAndGet();
                return n;
            }

            LooseRef n2 = scanRef(null, name);
            if (n2 == null)
                return packed.get(name);
            if (looseRefs.compareAndSet(curList, curList.add(idx, n2)))
                modCnt.incrementAndGet();
            return n2;
        }

        private LooseRef scanRef(LooseRef @ref, string name)
        {
            FileInfo path = fileFor(name);
            long modified = 0;

            modified = path.lastModified();

            if (@ref != null)
            {
                if (@ref.getLastModified() == modified)
                    return @ref;
                name = @ref.getName();
            }
            else if (modified == 0)
                return null;

            byte[] buf;
            try
            {
                buf = IO.ReadFully(path, 4096);
            }
            catch (FileNotFoundException)
            {
                return null; // doesn't exist; not a reference.
            }
            catch (DirectoryNotFoundException)
            {
                return null; // doesn't exist; not a reference.
            }

            int n = buf.Length;
            if (n == 0)
                return null; // empty file; not a reference.

            if (isSymRef(buf, n))
            {
                // trim trailing whitespace
                while (0 < n && Char.IsWhiteSpace((char)buf[n - 1]))
                    n--;
                if (n < 6)
                {
                    string content = RawParseUtils.decode(buf, 0, n);
                    throw new IOException("Not a ref: " + name + ": " + content);
                }
                string target = RawParseUtils.decode(buf, 5, n);
                return newSymbolicRef(modified, name, target);
            }

            if (n < Constants.OBJECT_ID_STRING_LENGTH)
                return null; // impossibly short object identifier; not a reference.

            ObjectId id;
            try
            {
                id = ObjectId.FromString(buf, 0);
            }
            catch (ArgumentException)
            {
                while (0 < n && Char.IsWhiteSpace((char)buf[n - 1]))
                    n--;
                string content = RawParseUtils.decode(buf, 0, n);
                throw new IOException("Not a ref: " + name + ": " + content);
            }
            return new LooseUnpeeled(modified, name, id);
        }

        private static bool isSymRef(byte[] buf, int n)
        {
            if (n < 6)
                return false;
            return buf[0] == 'r' //
                   && buf[1] == 'e' //
                   && buf[2] == 'f' //
                   && buf[3] == ':' //
                   && buf[4] == ' ';
        }

        /* If the parent should fire listeners, fires them. */
        private void fireRefsChanged()
        {
            int last = lastNotifiedModCnt.get();
            int curr = modCnt.get();
            if (last != curr && lastNotifiedModCnt.compareAndSet(last, curr))
                parent.fireRefsChanged();
        }

        /// <summary>
        /// Create a reference update to write a temporary reference.
        /// </summary>
        /// <returns>an update for a new temporary reference.</returns>
        public RefDirectoryUpdate newTemporaryUpdate()
        {
            FileInfo tmp = PathUtil.CombineFilePath(refsDir, "renamed_" + Guid.NewGuid() + "_ref");
            string name = Constants.R_REFS + tmp.Name;
            Ref @ref = new Unpeeled(Storage.New, name, null);
            return new RefDirectoryUpdate(this, @ref);
        }

        /// <summary>
        /// Locate the file on disk for a single reference name.
        /// </summary>
        /// <param name="name">
        /// name of the ref, relative to the Git repository top level
        /// directory (so typically starts with refs/).
        /// </param>
        /// <returns>the loose file location.</returns>
        public FileInfo fileFor(string name)
        {
            if (name.StartsWith(Constants.R_REFS))
            {
                name = name.Substring(Constants.R_REFS.Length);
                return PathUtil.CombineFilePath(refsDir, name);
            }
            return PathUtil.CombineFilePath(gitDir, name);
        }

        /// <summary>
        /// Locate the log file on disk for a single reference name.
        /// </summary>
        /// <param name="name">
        /// name of the ref, relative to the Git repository top level
        /// directory (so typically starts with refs/).
        /// </param>
        /// <returns>the log file location.</returns>
        public FileInfo logFor(string name)
        {
            if (name.StartsWith(Constants.R_REFS))
            {
                name = name.Substring(Constants.R_REFS.Length);
                return PathUtil.CombineFilePath(logsRefsDir, name);
            }
            return PathUtil.CombineFilePath(logsDir, name);
        }

        public static int levelsIn(string name)
        {
            int count = 0;
            for (int p = name.IndexOf('/'); p >= 0; p = name.IndexOf('/', p + 1))
                count++;
            return count;
        }

        public static void delete(FileInfo file, int depth)
        {
            if (!file.DeleteFile() && file.IsFile())
                throw new IOException("File cannot be deleted: " + file);

            DirectoryInfo dir = file.Directory;
            for (int i = 0; i < depth; ++i)
            {
                try
                {
                    dir.Delete();
                }
                catch (IOException)
                {
                    break;   // ignore problem here
                }

                dir = dir.Parent;
            }
        }

        private class PackedRefList : RefList<Ref>
        {
            public static PackedRefList NO_PACKED_REFS = new PackedRefList(RefList<Ref>
                                                                               .emptyList(), 0, 0);

            /* Last length of the packed-refs file when we read it. */
            public readonly long lastSize;

            /* Last modified time of the packed-refs file when we read it. */
            public readonly long lastModified;

            public PackedRefList(RefList<Ref> src, long size, long mtime)
                : base(src)
            {
                lastSize = size;
                lastModified = mtime;
            }
        }

        private static LooseSymbolicRef newSymbolicRef(long lastModified,
                                                       string name, string target)
        {
            Ref dst = new Unpeeled(Storage.New, target, null);
            return new LooseSymbolicRef(lastModified, name, dst);
        }

        private interface LooseRef : Ref
        {
            long getLastModified();

            LooseRef peel(ObjectIdRef newLeaf);
        }

        private class LoosePeeledTag : PeeledTag, LooseRef
        {
            private readonly long _lastModified;

            public LoosePeeledTag(long mtime, string refName, ObjectId id, ObjectId p)
                : base(Storage.Loose, refName, id, p)
            {
                _lastModified = mtime;
            }

            public long getLastModified()
            {
                return _lastModified;
            }

            public LooseRef peel(ObjectIdRef newLeaf)
            {
                return this;
            }
        }

        private class LooseNonTag : PeeledNonTag
                                     , LooseRef
        {
            private readonly long _lastModified;

            public LooseNonTag(long mtime, string refName, ObjectId id)
                : base(Storage.Loose, refName, id)
            {
                _lastModified = mtime;
            }

            public long getLastModified()
            {
                return _lastModified;
            }

            public LooseRef peel(ObjectIdRef newLeaf)
            {
                return this;
            }
        }

        private class LooseUnpeeled : Unpeeled, LooseRef
        {
            private readonly long _lastModified;

            public LooseUnpeeled(long mtime, string refName, ObjectId id)
                : base(Storage.Loose, refName, id)
            {
                _lastModified = mtime;
            }

            public long getLastModified()
            {
                return _lastModified;
            }

            public LooseRef peel(ObjectIdRef newLeaf)
            {
                if (newLeaf.getPeeledObjectId() != null)
                    return new LoosePeeledTag(_lastModified, Name,
                                              ObjectId, newLeaf.PeeledObjectId);
                else
                    return new LooseNonTag(_lastModified, Name, ObjectId);
            }
        }

        private class LooseSymbolicRef : SymbolicRef, LooseRef
        {
            private readonly long _lastModified;

            public LooseSymbolicRef(long mtime, string refName, Ref target)
                : base(refName, target)
            {
                _lastModified = mtime;
            }

            public long getLastModified()
            {
                return _lastModified;
            }

            public LooseRef peel(ObjectIdRef newLeaf)
            {
                // We should never try to peel the symbolic references.
                throw new NotSupportedException();
            }
        }
    }
}


