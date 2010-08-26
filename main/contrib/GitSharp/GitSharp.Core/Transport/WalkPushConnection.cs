/*
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
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Generic push support for dumb transport protocols.
    /// <para/>
    /// Since there are no Git-specific smarts on the remote side of the connection
    /// the client side must handle everything on its own. The generic push support
    /// requires being able to delete, create and overwrite files on the remote side,
    /// as well as create any missing directories (if necessary). Typically this can
    /// be handled through an FTP style protocol.
    /// <para/>
    /// Objects not on the remote side are uploaded as pack files, using one pack
    /// file per invocation. This simplifies the implementation as only two data
    /// files need to be written to the remote repository.
    /// <para/>
    /// Push support supplied by this class is not multiuser safe. Concurrent pushes
    /// to the same repository may yield an inconsistent reference database which may
    /// confuse fetch clients.
    /// <para/>
    /// A single push is concurrently safe with multiple fetch requests, due to the
    /// careful order of operations used to update the repository. Clients fetching
    /// may receive transient failures due to short reads on certain files if the
    /// protocol does not support atomic file replacement.
    /// 
    /// see <see cref="WalkRemoteObjectDatabase"/>.
    /// </summary>
    public class WalkPushConnection : BaseConnection, IPushConnection
    {
        /// <summary>
        /// The repository this transport pushes out of.
        /// </summary>
        private readonly Repository _local;

        /// <summary>
        /// Location of the remote repository we are writing to.
        /// </summary>
        private readonly URIish _uri;

        /// <summary>
        /// Database connection to the remote repository.
        /// </summary>
        private readonly WalkRemoteObjectDatabase _dest;

        /// <summary>
        /// Packs already known to reside in the remote repository.
        /// </summary>
        private IDictionary<string, string> _packNames;

        /// <summary>
        /// Complete listing of refs the remote will have after our push.
        /// </summary>
        private IDictionary<string, Ref> _newRefs;

        /// <summary>
        /// Updates which require altering the packed-refs file to complete.
        /// <para/>
        /// If this collection is non-empty then any refs listed in <see cref="_newRefs"/>
        /// with a storage class of <see cref="Storage.Packed"/> will be written.
        /// </summary>
        private ICollection<RemoteRefUpdate> _packedRefUpdates;

        public WalkPushConnection(IWalkTransport walkTransport, WalkRemoteObjectDatabase w)
        {
            var t = (Transport)walkTransport;
            _local = t.Local;
            _uri = t.Uri;
            _dest = w;
        }

        public void Push(ProgressMonitor monitor, IDictionary<string, RemoteRefUpdate> refUpdates)
        {
            if (refUpdates == null)
                throw new ArgumentNullException("refUpdates");

            markStartedOperation();
            _packNames = null;
            _newRefs = new Dictionary<string, Ref>(RefsMap);
            _packedRefUpdates = new List<RemoteRefUpdate>(refUpdates.Count);

            // Filter the commands and issue all deletes first. This way we
            // can correctly handle a directory being cleared out and a new
            // ref using the directory name being created.
            //
            var updates = new List<RemoteRefUpdate>();
            foreach (RemoteRefUpdate u in refUpdates.Values)
            {
                string n = u.RemoteName;
                if (!n.StartsWith("refs/") || !Repository.IsValidRefName(n))
                {
                    u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                    u.Message = "funny refname";
                    continue;
                }

                if (AnyObjectId.equals(ObjectId.ZeroId, u.NewObjectId))
                {
                    DeleteCommand(u);
                }
                else
                {
                    updates.Add(u);
                }
            }

            // If we have any updates we need to upload the objects first, to
            // prevent creating refs pointing at non-existent data. Then we
            // can update the refs, and the info-refs file for dumb transports.
            //
            if (!updates.isEmpty())
            {
                Sendpack(updates, monitor);
            }
            foreach (RemoteRefUpdate u in updates)
            {
                UpdateCommand(u);
            }

            // Is this a new repository? If so we should create additional
            // metadata files so it is properly initialized during the push.
            //
            if (!updates.isEmpty() && IsNewRepository)
            {
                CreateNewRepository(updates);
            }

            RefWriter refWriter = new PushRefWriter(_newRefs.Values, _dest);
            if (_packedRefUpdates.Count > 0)
            {
                try
                {
                    refWriter.writePackedRefs();
                    foreach (RemoteRefUpdate u in _packedRefUpdates)
                    {
                        u.Status = RemoteRefUpdate.UpdateStatus.OK;
                    }
                }
                catch (IOException e)
                {
                    foreach (RemoteRefUpdate u in _packedRefUpdates)
                    {
                        u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                        u.Message = e.Message;
                    }
                    throw new TransportException(_uri, "failed updating refs", e);
                }
            }

            try
            {
                refWriter.writeInfoRefs();
            }
            catch (IOException err)
            {
                throw new TransportException(_uri, "failed updating refs", err);
            }
        }

        public override void Close()
        {
            _dest.Dispose();
#if DEBUG
            GC.SuppressFinalize(this); // Disarm lock-release checker
#endif
        }

#if DEBUG
        // A debug mode warning if the type has not been disposed properly
        ~WalkPushConnection()
        {
            Console.Error.WriteLine(GetType().Name + " has not been properly disposed: " + this._uri);
        }
#endif


        private void Sendpack(IEnumerable<RemoteRefUpdate> updates, ProgressMonitor monitor)
        {
            string pathPack = null;
            string pathIdx = null;

            try
            {
                var pw = new PackWriter(_local, monitor);
                var need = new List<ObjectId>();
                var have = new List<ObjectId>();

                foreach (RemoteRefUpdate r in updates)
                {
                    need.Add(r.NewObjectId);
                }

                foreach (Ref r in Refs)
                {
                    have.Add(r.ObjectId);
                    if (r.PeeledObjectId != null)
                    {
                        have.Add(r.PeeledObjectId);
                    }
                }
                pw.preparePack(need, have);

                // We don't have to continue further if the pack will
                // be an empty pack, as the remote has all objects it
                // needs to complete this change.
                //
                if (pw.getObjectsNumber() == 0) return;

                _packNames = new Dictionary<string, string>();
                foreach (string n in _dest.getPackNames())
                {
                    _packNames.put(n, n);
                }

                string b = "pack-" + pw.computeName().Name;
                string packName = b + IndexPack.PackSuffix;
                pathPack = "pack/" + packName;
                pathIdx = "pack/" + b + IndexPack.IndexSuffix;

                if (_packNames.remove(packName) != null)
                {
                    // The remote already contains this pack. We should
                    // remove the index before overwriting to prevent bad
                    // offsets from appearing to clients.
                    //
                    _dest.writeInfoPacks(_packNames.Keys);
                    _dest.deleteFile(pathIdx);
                }

                // Write the pack file, then the index, as readers look the
                // other direction (index, then pack file).
                //
                string wt = "Put " + b.Slice(0, 12);
                using (Stream os = _dest.writeFile(pathPack, monitor, wt + "." + IndexPack.PackSuffix))
                {
                    pw.writePack(os);
                }

                using (Stream os = _dest.writeFile(pathIdx, monitor, wt + "." + IndexPack.IndexSuffix))
                {
                    pw.writeIndex(os);
                }

                // Record the pack at the start of the pack info list. This
                // way clients are likely to consult the newest pack first,
                // and discover the most recent objects there.
                //
                var infoPacks = new List<string> { packName };
                infoPacks.AddRange(_packNames.Keys);
                _dest.writeInfoPacks(infoPacks);
            }
            catch (IOException err)
            {
                SafeDelete(pathIdx);
                SafeDelete(pathPack);

                throw new TransportException(_uri, "cannot store objects", err);
            }
        }

        private void SafeDelete(string path)
        {
            if (path != null)
            {
                try
                {
                    _dest.deleteFile(path);
                }
                catch (IOException)
                {
                    // Ignore the deletion failure. We probably are
                    // already failing and were just trying to pick
                    // up after ourselves.
                }
            }
        }

        private void DeleteCommand(RemoteRefUpdate u)
        {
            Ref r = _newRefs.remove(u.RemoteName);

            if (r == null)
            {
                // Already gone.
                //
                u.Status = RemoteRefUpdate.UpdateStatus.OK;
                return;
            }

            if (r.StorageFormat.IsPacked)
            {
                _packedRefUpdates.Add(u);
            }

            if (r.StorageFormat.IsLoose)
            {
                try
                {
                    _dest.deleteRef(u.RemoteName);
                    u.Status = RemoteRefUpdate.UpdateStatus.OK;
                }
                catch (IOException e)
                {
                    u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                    u.Message = e.Message;
                }
            }

            try
            {
                _dest.deleteRefLog(u.RemoteName);
            }
            catch (IOException e)
            {
                u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                u.Message = e.Message;
            }
        }

        private void UpdateCommand(RemoteRefUpdate u)
        {
            try
            {
                _dest.writeRef(u.RemoteName, u.NewObjectId);
                _newRefs.put(u.RemoteName, new Unpeeled(Storage.Loose, u.RemoteName, u.NewObjectId));
                u.Status = RemoteRefUpdate.UpdateStatus.OK;
            }
            catch (IOException e)
            {
                u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                u.Message = e.Message;
            }
        }

        private bool IsNewRepository
        {
            get { return RefsMap.Count == 0 && _packNames != null && _packNames.Count == 0; }
        }

        private void CreateNewRepository(IList<RemoteRefUpdate> updates)
        {
            try
            {
                string @ref = "ref: " + PickHead(updates) + "\n";
                byte[] bytes = Constants.encode(@ref);
                _dest.writeFile(WalkRemoteObjectDatabase.ROOT_DIR + Constants.HEAD, bytes);
            }
            catch (IOException e)
            {
                throw new TransportException(_uri, "Cannot Create HEAD", e);
            }

            try
            {
                const string config = "[core]\n\trepositoryformatversion = 0\n";
                byte[] bytes = Constants.encode(config);
                _dest.writeFile(WalkRemoteObjectDatabase.ROOT_DIR + "config", bytes);
            }
            catch (IOException e)
            {
                throw new TransportException(_uri, "Cannot Create config", e);
            }
        }

        private static string PickHead(IList<RemoteRefUpdate> updates)
        {
            // Try to use master if the user is pushing that, it is the
            // default branch and is likely what they want to remain as
            // the default on the new remote.
            //
            foreach (RemoteRefUpdate u in updates)
            {
                string n = u.RemoteName;
                if (n.Equals(Constants.R_HEADS + Constants.MASTER))
                {
                    return n;
                }
            }

            // Pick any branch, under the assumption the user pushed only
            // one to the remote side.
            //
            foreach (RemoteRefUpdate u in updates)
            {
                string n = u.RemoteName;
                if (n.StartsWith(Constants.R_HEADS))
                {
                    return n;
                }
            }

            return updates[0].RemoteName;
        }

        #region Nested Types

        private class PushRefWriter : RefWriter
        {
            private readonly WalkRemoteObjectDatabase _dest;

            public PushRefWriter(IEnumerable<Ref> refs, WalkRemoteObjectDatabase dest)
                : base(refs)
            {
                _dest = dest;
            }

            protected override void writeFile(string file, byte[] content)
            {
                _dest.writeFile(WalkRemoteObjectDatabase.ROOT_DIR + file, content);
            }
        }

        #endregion
    }
}