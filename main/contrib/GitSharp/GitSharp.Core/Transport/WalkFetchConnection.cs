/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Generic fetch support for dumb transport protocols.
    /// <para/>
    /// Since there are no Git-specific smarts on the remote side of the connection
    /// the client side must determine which objects it needs to copy in order to
    /// completely fetch the requested refs and their history. The generic walk
    /// support in this class parses each individual object (once it has been copied
    /// to the local repository) and examines the list of objects that must also be
    /// copied to create a complete history. Objects which are already available
    /// locally are retained (and not copied), saving bandwidth for incremental
    /// fetches. Pack files are copied from the remote repository only as a last
    /// resort, as the entire pack must be copied locally in order to access any
    /// single object.
    /// <para/>
    /// This fetch connection does not actually perform the object data transfer.
    /// Instead it delegates the transfer to a <see cref="WalkRemoteObjectDatabase"/>,
    /// which knows how to read individual files from the remote repository and
    /// supply the data as a standard Java InputStream.
    /// </summary>
    public class WalkFetchConnection : BaseFetchConnection, IDisposable
    {
        /// <summary>
        /// The repository this transport fetches into, or pushes out of.
        /// </summary>
        private readonly Repository _local;

        /// <summary>
        /// If not null the validator for received objects.
        /// </summary>
        private readonly ObjectChecker _objCheck;

        /// <summary>
        /// List of all remote repositories we may need to get objects out of.
        /// <para/>
        /// The first repository in the list is the one we were asked to fetch from;
        /// the remaining repositories point to the alternate locations we can fetch
        /// objects through.
        /// </summary>
        private readonly List<WalkRemoteObjectDatabase> _remotes;

        /// <summary>
        /// Most recently used item in <see cref="_remotes"/>.
        /// </summary>
        private int _lastRemoteIdx;

        private readonly RevWalk.RevWalk _revWalk;
        private readonly TreeWalk.TreeWalk _treeWalk;

        /// <summary>
        /// Objects whose direct dependents we know we have (or will have).
        /// </summary>
        private readonly RevFlag COMPLETE;

        /// <summary>
        /// Objects that have already entered <see cref="_workQueue"/>.
        /// </summary>
        private readonly RevFlag IN_WORK_QUEUE;

        /// <summary>
        /// Commits that have already entered <see cref="_localCommitQueue"/>.
        /// </summary>
        private readonly RevFlag LOCALLY_SEEN;

        /// <summary>
        /// Commits already reachable from all local refs.
        /// </summary>
        private readonly DateRevQueue _localCommitQueue;

        /// <summary>
        /// Objects we need to copy from the remote repository.
        /// </summary>
        private LinkedList<ObjectId> _workQueue;

        /// <summary>
        /// Databases we have not yet obtained the list of packs from.
        /// </summary>
        private readonly LinkedList<WalkRemoteObjectDatabase> _noPacksYet;

        /// <summary>
        /// Databases we have not yet obtained the alternates from.
        /// </summary>
        private readonly LinkedList<WalkRemoteObjectDatabase> _noAlternatesYet;

        /// <summary>
        /// Packs we have discovered, but have not yet fetched locally.
        /// </summary>
        private readonly LinkedList<RemotePack> _unfetchedPacks;

        /// <summary>
        /// Packs whose indexes we have looked at in <see cref="_unfetchedPacks"/>.
        /// <para/>
        /// We try to avoid getting duplicate copies of the same pack through
        /// multiple alternates by only looking at packs whose names are not yet in
        /// this collection.
        /// </summary>
        private readonly List<string> _packsConsidered;

        private readonly MutableObjectId _idBuffer = new MutableObjectId();

        private readonly MessageDigest _objectDigest = Constants.newMessageDigest();

        /// <summary>
        /// Errors received while trying to obtain an object.
        /// <para/>
        /// If the fetch winds up failing because we cannot locate a specific object
        /// then we need to report all errors related to that object back to the
        /// caller as there may be cascading failures.
        /// </summary>
        private readonly Dictionary<ObjectId, List<Exception>> _fetchErrors;

        private string _lockMessage;

        private readonly List<PackLock> _packLocks;

        public WalkFetchConnection(IWalkTransport t, WalkRemoteObjectDatabase w)
        {
            var wt = (Transport)t;
            _local = wt.Local;
            _objCheck = wt.CheckFetchedObjects ? new ObjectChecker() : null;

            _remotes = new List<WalkRemoteObjectDatabase> { w };

            _unfetchedPacks = new LinkedList<RemotePack>();
            _packsConsidered = new List<string>();

            _noPacksYet = new LinkedList<WalkRemoteObjectDatabase>();
            _noPacksYet.AddLast(w);

            _noAlternatesYet = new LinkedList<WalkRemoteObjectDatabase>();
            _noAlternatesYet.AddLast(w);

            _fetchErrors = new Dictionary<ObjectId, List<Exception>>();
            _packLocks = new List<PackLock>(4);

            _revWalk = new RevWalk.RevWalk(_local);
            _revWalk.setRetainBody(false);
            _treeWalk = new TreeWalk.TreeWalk(_local);

            COMPLETE = _revWalk.newFlag("COMPLETE");
            IN_WORK_QUEUE = _revWalk.newFlag("IN_WORK_QUEUE");
            LOCALLY_SEEN = _revWalk.newFlag("LOCALLY_SEEN");

            _localCommitQueue = new DateRevQueue();
            _workQueue = new LinkedList<ObjectId>();
        }

        public override bool DidFetchTestConnectivity
        {
            get
            {
                return true;
            }
        }

        protected override void doFetch(ProgressMonitor monitor, ICollection<Ref> want, IList<ObjectId> have)
        {
            MarkLocalRefsComplete(have);
            QueueWants(want);

            while (!monitor.IsCancelled && _workQueue.Count > 0)
            {
                ObjectId id = _workQueue.First.Value;
                _workQueue.RemoveFirst();
                var ro = (id as RevObject);
                if (ro == null || !ro.has(COMPLETE))
                {
                    DownloadObject(monitor, id);
                }
                Process(id);
            }
        }

        public override List<PackLock> PackLocks
        {
            get { return _packLocks; }
        }

        public override void SetPackLockMessage(string message)
        {
            _lockMessage = message;
        }

        public override void Close()
        {
            foreach (RemotePack p in _unfetchedPacks)
            {
                p.TmpIdx.DeleteFile();
            }
            foreach (WalkRemoteObjectDatabase r in _remotes)
            {
                r.Dispose();
            }
        }

        private void QueueWants(IEnumerable<Ref> want)
        {
            var inWorkQueue = new List<ObjectId>();
            foreach (Ref r in want)
            {
                ObjectId id = r.ObjectId;
                try
                {
                    RevObject obj = _revWalk.parseAny(id);
                    if (obj.has(COMPLETE))
                        continue;
                    bool contains = inWorkQueue.Contains(id);
                    inWorkQueue.Add(id);
                    if (!contains)
                    {
                        obj.add(IN_WORK_QUEUE);
                        _workQueue.AddLast(obj);
                    }
                }
                catch (MissingObjectException)
                {
                    bool contains = inWorkQueue.Contains(id);
                    inWorkQueue.Add(id);
                    if (!contains)
                        _workQueue.AddLast(id);
                }
                catch (IOException e)
                {
                    throw new TransportException("Cannot Read " + id.Name, e);
                }
            }
        }

        private void Process(ObjectId id)
        {
            RevObject obj;
            try
            {
                if (id is RevObject)
                {
                    obj = (RevObject)id;
                    if (obj.has(COMPLETE))
                        return;
                    _revWalk.parseHeaders(obj);
                }
                else
                {
                    obj = _revWalk.parseAny(id);
                    if (obj.has(COMPLETE))
                        return;
                }
            }
            catch (IOException e)
            {
                throw new TransportException("Cannot Read " + id.Name, e);
            }

            switch (obj.Type)
            {
                case Constants.OBJ_BLOB:
                    ProcessBlob(obj);
                    break;

                case Constants.OBJ_TREE:
                    ProcessTree(obj);
                    break;

                case Constants.OBJ_COMMIT:
                    ProcessCommit(obj);
                    break;

                case Constants.OBJ_TAG:
                    ProcessTag(obj);
                    break;

                default:
                    throw new TransportException("Unknown object type " + id.Name + " (" + obj.Type + ")");
            }

            // If we had any prior errors fetching this object they are
            // now resolved, as the object was parsed successfully.
            //
            _fetchErrors.Remove(id.Copy());
        }

        private void ProcessBlob(RevObject obj)
        {
            if (!_local.HasObject(obj))
            {
                throw new TransportException("Cannot Read blob " + obj.Name, new MissingObjectException(obj, Constants.TYPE_BLOB));
            }
            obj.add(COMPLETE);
        }

        private void ProcessTree(RevObject obj)
        {
            try
            {
                _treeWalk.reset(obj);
                while (_treeWalk.next())
                {
                    FileMode mode = _treeWalk.getFileMode(0);
                    int sType = (int)mode.ObjectType;

                    switch (sType)
                    {
                        case Constants.OBJ_BLOB:
                        case Constants.OBJ_TREE:
                            _treeWalk.getObjectId(_idBuffer, 0);
                            Needs(_revWalk.lookupAny(_idBuffer, sType));
                            continue;

                        default:
                            if (FileMode.GitLink.Equals(sType))
                                continue;
                            _treeWalk.getObjectId(_idBuffer, 0);
                            throw new CorruptObjectException("Invalid mode " + mode.ObjectType + " for " + _idBuffer.Name + " " +
                                                             _treeWalk.getPathString() + " in " + obj.getId().Name + ".");
                    }
                }
            }
            catch (IOException ioe)
            {
                throw new TransportException("Cannot Read tree " + obj.Name, ioe);
            }
            obj.add(COMPLETE);
        }

        private void ProcessCommit(RevObject obj)
        {
            var commit = (RevCommit)obj;
            MarkLocalCommitsComplete(commit.CommitTime);
            Needs(commit.Tree);
            foreach (RevCommit p in commit.Parents)
            {
                Needs(p);
            }
            obj.add(COMPLETE);
        }

        private void ProcessTag(RevObject obj)
        {
            var tag = (RevTag)obj;
            Needs(tag.getObject());
            obj.add(COMPLETE);
        }

        private void Needs(RevObject obj)
        {
            if (obj.has(COMPLETE)) return;

            if (!obj.has(IN_WORK_QUEUE))
            {
                obj.add(IN_WORK_QUEUE);
                _workQueue.AddLast(obj);
            }
        }

        private void DownloadObject(ProgressMonitor pm, AnyObjectId id)
        {
            if (_local.HasObject(id)) return;

            while (true)
            {
                // Try a pack file we know about, but don't have yet. Odds are
                // that if it has this object, it has others related to it so
                // getting the pack is a good bet.
                //
                if (DownloadPackedObject(pm, id))
                    return;

                // Search for a loose object over all alternates, starting
                // from the one we last successfully located an object through.
                //
                string idStr = id.Name;
                string subdir = idStr.Slice(0, 2);
                string file = idStr.Substring(2);
                string looseName = subdir + "/" + file;

                for (int i = _lastRemoteIdx; i < _remotes.Count; i++)
                {
                    if (DownloadLooseObject(id, looseName, _remotes[i]))
                    {
                        _lastRemoteIdx = i;
                        return;
                    }
                }

                for (int i = 0; i < _lastRemoteIdx; i++)
                {
                    if (DownloadLooseObject(id, looseName, _remotes[i]))
                    {
                        _lastRemoteIdx = i;
                        return;
                    }
                }

                // Try to obtain more pack information and search those.
                //
                while (_noPacksYet.Count > 0)
                {
                    WalkRemoteObjectDatabase wrr = _noPacksYet.First.Value;
                    _noPacksYet.RemoveFirst();
                    ICollection<string> packNameList;
                    try
                    {
                        pm.BeginTask("Listing packs", ProgressMonitor.UNKNOWN);
                        packNameList = wrr.getPackNames();
                    }
                    catch (IOException e)
                    {
                        // Try another repository.
                        //
                        RecordError(id, e);
                        continue;
                    }
                    finally
                    {
                        pm.EndTask();
                    }

                    if (packNameList == null || packNameList.Count == 0)
                        continue;
                    foreach (string packName in packNameList)
                    {
                        bool contains = _packsConsidered.Contains(packName);
                        _packsConsidered.Add(packName);
                        if (!contains)
                        {
                            _unfetchedPacks.AddLast(new RemotePack(_lockMessage, _packLocks, _objCheck, _local, wrr, packName));
                        }
                    }
                    if (DownloadPackedObject(pm, id))
                        return;
                }

                // Try to expand the first alternate we haven't expanded yet.
                //
                ICollection<WalkRemoteObjectDatabase> al = ExpandOneAlternate(id, pm);
                if (al != null && al.Count > 0)
                {
                    foreach (WalkRemoteObjectDatabase alt in al)
                    {
                        _remotes.Add(alt);
                        _noPacksYet.AddLast(alt);
                        _noAlternatesYet.AddLast(alt);
                    }
                    continue;
                }

                // We could not obtain the object. There may be reasons why.
                //
                List<Exception> failures = _fetchErrors.get(id.Copy());

                var te = new TransportException("Cannot get " + id.Name + ".");

                if (failures != null && failures.Count > 0)
                {
                    te = failures.Count == 1 ?
                        new TransportException("Cannot get " + id.Name + ".", failures[0]) :
                        new TransportException("Cannot get " + id.Name + ".", new CompoundException(failures));
                }

                throw te;
            }
        }

        private bool DownloadPackedObject(ProgressMonitor monitor, AnyObjectId id)
        {
            // Search for the object in a remote pack whose index we have,
            // but whose pack we do not yet have.
            //
            var iter = new LinkedListIterator<RemotePack>(_unfetchedPacks);
            
            while (iter.hasNext() && !monitor.IsCancelled)
            {
                RemotePack pack = iter.next();
                try
                {
                    pack.OpenIndex(monitor);
                }
                catch (IOException err)
                {
                    // If the index won't open its either not found or
                    // its a format we don't recognize. In either case
                    // we may still be able to obtain the object from
                    // another source, so don't consider it a failure.
                    //                    
                    RecordError(id, err);
                    iter.remove();
                    continue;
                }

                if (monitor.IsCancelled)
                {
                    // If we were cancelled while the index was opening
                    // the open may have aborted. We can't search an
                    // unopen index.
                    //
                    return false;
                }

                if (!pack.Index.HasObject(id))
                {	
                    // Not in this pack? Try another.
                    //
                    continue;
                }

                // It should be in the associated pack. Download that
                // and attach it to the local repository so we can use
                // all of the contained objects.
                //
                try
                {
                    pack.DownloadPack(monitor);
                }
                catch (IOException err)
                {
                    // If the pack failed to download, index correctly,
                    // or open in the local repository we may still be
                    // able to obtain this object from another pack or
                    // an alternate.
                    //
                    RecordError(id, err);
                    continue;
                }
                finally
                {
                    // If the pack was good its in the local repository
                    // and Repository.hasObject(id) will succeed in the
                    // future, so we do not need this data anymore. If
                    // it failed the index and pack are unusable and we
                    // shouldn't consult them again.
                    //
                    pack.TmpIdx.DeleteFile();
                    iter.remove();
                }

                if (!_local.HasObject(id))
                {
                    // What the hell? This pack claimed to have
                    // the object, but after indexing we didn't
                    // actually find it in the pack.
                    //
                    RecordError(id,
                                new FileNotFoundException("Object " + id.Name + " not found in " + pack.PackName + "."));
                    continue;
                }

                // Complete any other objects that we can.
                //
                IIterator<ObjectId> pending = SwapFetchQueue();
                while (pending.hasNext())
                {
                    ObjectId p = pending.next();
                    if (pack.Index.HasObject(p))
                    {
                        pending.remove();
                        Process(p);
                    }
                    else
                        _workQueue.AddLast(p);
                }
                return true;
            }
            return false;
        }

        private IIterator<ObjectId> SwapFetchQueue()
        {
            var r = new LinkedListIterator<ObjectId>(_workQueue);
            _workQueue = new LinkedList<ObjectId>();
            return r;
        }

        private bool DownloadLooseObject(AnyObjectId id, string looseName, WalkRemoteObjectDatabase remote)
        {
            try
            {
                byte[] compressed = remote.open(looseName).toArray();
                VerifyLooseObject(id, compressed);
                SaveLooseObject(id, compressed);
                return true;
            }
            catch (FileNotFoundException e)
            {
                // Not available in a loose format from this alternate?
                // Try another strategy to get the object.
                //
                RecordError(id, e);
                return false;
            }
            catch (IOException e)
            {
                throw new TransportException("Cannot download " + id.Name, e);
            }
        }

        private void VerifyLooseObject(AnyObjectId id, byte[] compressed)
        {
            UnpackedObjectLoader uol;
            try
            {
                uol = new UnpackedObjectLoader(compressed);
            }
            catch (CorruptObjectException parsingError)
            {
                // Some HTTP servers send back a "200 OK" status with an HTML
                // page that explains the requested file could not be found.
                // These servers are most certainly misconfigured, but many
                // of them exist in the world, and many of those are hosting
                // Git repositories.
                //
                // Since an HTML page is unlikely to hash to one of our loose
                // objects we treat this condition as a FileNotFoundException
                // and attempt to recover by getting the object from another
                // source.
                //
                var e = new FileNotFoundException(id.Name, parsingError);
                throw e;
            }

            _objectDigest.Reset();
            _objectDigest.Update(Constants.encodedTypeString(uol.Type));
            _objectDigest.Update((byte)' ');
            _objectDigest.Update(Constants.encodeASCII(uol.Size));
            _objectDigest.Update(0);
            _objectDigest.Update(uol.CachedBytes);
            _idBuffer.FromRaw(_objectDigest.Digest(), 0);

            if (!AnyObjectId.equals(id, _idBuffer)) 
            {
                throw new TransportException("Incorrect hash for " + id.Name + "; computed " + _idBuffer.Name + " as a " +
                                             Constants.typeString(uol.Type) + " from " + compressed.Length +
                                             " bytes.");
            }
            if (_objCheck != null)
            {
                try
                {
                    _objCheck.check(uol.Type, uol.CachedBytes);
                }
                catch (CorruptObjectException e)
                {
                    throw new TransportException("Invalid " + Constants.typeString(uol.Type) + " " + id.Name + ": " + e.Message);
                }
            }
        }

        private void SaveLooseObject(AnyObjectId id, byte[] compressed)
        {
            var tmpPath = Path.Combine(_local.ObjectsDirectory.FullName, Path.GetTempFileName());

            try
            {
                File.WriteAllBytes(tmpPath, compressed);

                var tmp = new FileInfo(tmpPath);
                tmp.Attributes |= FileAttributes.ReadOnly;
            }
            catch (IOException)
            {
                File.Delete(tmpPath);
                throw;
            }

            FileInfo o = _local.ToFile(id);
            if (new FileInfo(tmpPath).RenameTo(o.FullName))
                return;

            // Maybe the directory doesn't exist yet as the object
            // directories are always lazily created. Note that we
            // try the rename first as the directory likely does exist.
            //
            o.Directory.Mkdirs();
            if (new FileInfo(tmpPath).RenameTo(o.FullName))
                return;

            new FileInfo(tmpPath).DeleteFile();
            if (_local.HasObject(id))
                return;

            throw new ObjectWritingException("Unable to store " + id.Name + ".");
        }

        private ICollection<WalkRemoteObjectDatabase> ExpandOneAlternate(AnyObjectId id, ProgressMonitor pm)
        {
            while (_noAlternatesYet.Count > 0)
            {
                WalkRemoteObjectDatabase wrr = _noAlternatesYet.First.Value;
                _noAlternatesYet.RemoveFirst();
                try
                {
                    pm.BeginTask("Listing alternates", ProgressMonitor.UNKNOWN);
                    ICollection<WalkRemoteObjectDatabase> altList = wrr.getAlternates();
                    if (altList != null && altList.Count > 0)
                        return altList;
                }
                catch (IOException e)
                {
                    // Try another repository.
                    //
                    RecordError(id, e);
                }
                finally
                {
                    pm.EndTask();
                }
            }
            return null;
        }

        private void MarkLocalRefsComplete(IEnumerable<ObjectId> have)
        {
            foreach (Ref r in _local.getAllRefs().Values)
            {
                try
                {
                    MarkLocalObjComplete(_revWalk.parseAny(r.ObjectId));
                }
                catch (IOException readError)
                {
                    throw new TransportException("Local ref " + r.Name + " is missing object(s).", readError);
                }
            }

            foreach (ObjectId id in have)
            {
                try
                {
                    MarkLocalObjComplete(_revWalk.parseAny(id));
                }
                catch (IOException readError)
                {
                    throw new TransportException("Missing assumed " + id.Name, readError);
                }
            }
        }

        private void MarkLocalObjComplete(RevObject obj)
        {
            while (obj.Type == Constants.OBJ_TAG)
            {
                obj.add(COMPLETE);
                obj = ((RevTag)obj).getObject();
                _revWalk.parseHeaders(obj);
            }

            switch (obj.Type)
            {
                case Constants.OBJ_BLOB:
                    obj.add(COMPLETE);
                    break;

                case Constants.OBJ_COMMIT:
                    PushLocalCommit((RevCommit)obj);
                    break;

                case Constants.OBJ_TREE:
                    MarkTreeComplete((RevTree)obj);
                    break;
            }
        }

        private void MarkLocalCommitsComplete(int until)
        {
            try
            {
                while (true)
                {
                    RevCommit c = _localCommitQueue.peek();
                    if (c == null || c.CommitTime < until) return;
                    _localCommitQueue.next();

                    MarkTreeComplete(c.Tree);
                    foreach (RevCommit p in c.Parents)
                    {
                        PushLocalCommit(p);
                    }
                }
            }
            catch (IOException err)
            {
                throw new TransportException("Local objects incomplete.", err);
            }
        }

        private void PushLocalCommit(RevCommit p)
        {
            if (p.has(LOCALLY_SEEN)) return;
            _revWalk.parseHeaders(p);
            p.add(LOCALLY_SEEN);
            p.add(COMPLETE);
            p.carry(COMPLETE);
            _localCommitQueue.add(p);
        }

        private void MarkTreeComplete(RevTree tree)
        {
            if (tree.has(COMPLETE)) return;

            tree.add(COMPLETE);
            _treeWalk.reset(tree);
            while (_treeWalk.next())
            {
                FileMode mode = _treeWalk.getFileMode(0);
                int sType = (int)mode.ObjectType;

                switch (sType)
                {
                    case Constants.OBJ_BLOB:
                        _treeWalk.getObjectId(_idBuffer, 0);
                        _revWalk.lookupAny(_idBuffer, sType).add(COMPLETE);
                        continue;

                    case Constants.OBJ_TREE:
                        {
                            _treeWalk.getObjectId(_idBuffer, 0);
                            RevObject o = _revWalk.lookupAny(_idBuffer, sType);
                            if (!o.has(COMPLETE))
                            {
                                o.add(COMPLETE);
                                _treeWalk.enterSubtree();
                            }
                            continue;
                        }

                    default:
                        if (FileMode.GitLink.Equals(sType))
                            continue;
                        _treeWalk.getObjectId(_idBuffer, 0);
                        throw new CorruptObjectException("Invalid mode " + mode.ObjectType + " for " + _idBuffer.Name + " " +
                                                         _treeWalk.getPathString() + " in " + tree.Name);
                }
            }
        }

        private void RecordError(AnyObjectId id, Exception what)
        {
            ObjectId objId = id.Copy();
            List<Exception> errors = _fetchErrors.get(objId);
            if (errors == null)
            {
                errors = new List<Exception>(2);
                _fetchErrors.put(objId, errors);
            }
            errors.Add(what);
        }

        #region Nested Types

        private class RemotePack
        {
            private readonly WalkRemoteObjectDatabase _connection;
            private readonly string _idxName;
            private readonly Repository _local;
            private readonly ObjectChecker _objCheck;
            private readonly string _lockMessage;
            private readonly List<PackLock> _packLocks;

            public string PackName { get; private set; }
            public FileInfo TmpIdx { get; private set; }
            public PackIndex Index { get; private set; }

            public RemotePack(string lockMessage, List<PackLock> packLocks, ObjectChecker oC, Repository r, WalkRemoteObjectDatabase c, string pn)
            {
                _lockMessage = lockMessage;
                _packLocks = packLocks;
                _objCheck = oC;
                _local = r;
                DirectoryInfo objdir = _local.ObjectsDirectory;
                _connection = c;
                PackName = pn;
                _idxName = IndexPack.GetIndexFileName(PackName.Slice(0, PackName.Length - 5));

                string tn = _idxName;
                if (tn.StartsWith("pack-"))
                {
                    tn = tn.Substring(5);
                }

                if (tn.EndsWith(IndexPack.IndexSuffix))
                {
                    tn = tn.Slice(0, tn.Length - 4);
                }

                TmpIdx = new FileInfo(Path.Combine(objdir.ToString(), "walk-" + tn + ".walkidx"));
            }

            public void OpenIndex(ProgressMonitor pm)
            {
                if (Index != null) return;

                if (TmpIdx.IsFile())
                {
                    try
                    {
                        Index = PackIndex.Open(TmpIdx);
                        return;
                    }
                    catch (FileNotFoundException)
                    {
                        // Fall through and get the file.
                    }
                }

                using (Stream s = _connection.open("pack/" + _idxName))
                {
                    pm.BeginTask("Get " + _idxName.Slice(0, 12) + "..idx", !s.CanSeek ? ProgressMonitor.UNKNOWN : (int)(s.Length / 1024));

                    try
                    {
                        using (var fos = new FileStream(TmpIdx.FullName, System.IO.FileMode.CreateNew, FileAccess.Write))
                        {
                            var buf = new byte[2048];
                            int cnt;
                            while (!pm.IsCancelled && (cnt = s.Read(buf, 0, buf.Length)) > 0)
                            {
                                fos.Write(buf, 0, cnt);
                                pm.Update(cnt / 1024);
                            }
                        }
                    }
                    catch (IOException)
                    {
                        TmpIdx.DeleteFile();
                        throw;
                    }
                }

                pm.EndTask();

                if (pm.IsCancelled)
                {
                    TmpIdx.DeleteFile();
                    return;
                }

                try
                {
                    Index = PackIndex.Open(TmpIdx);
                }
                catch (IOException)
                {
                    TmpIdx.DeleteFile();
                    throw;
                }
            }

            public void DownloadPack(ProgressMonitor monitor)
            {
                Stream s = _connection.open("pack/" + PackName);
                IndexPack ip = IndexPack.Create(_local, s);
                ip.setFixThin(false);
                ip.setObjectChecker(_objCheck);
                ip.index(monitor);
                PackLock keep = ip.renameAndOpenPack(_lockMessage);
                if (keep != null)
                {
                    _packLocks.Add(keep);
                }
            }
        }

        #endregion

        public override void Dispose()
        {
            _revWalk.Dispose();
            COMPLETE.Dispose();
            IN_WORK_QUEUE.Dispose();
            LOCALLY_SEEN.Dispose();
            _objectDigest.Dispose();

            base.Dispose();
        }

    }
}